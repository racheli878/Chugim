using Chugim.Application.Common;
using Chugim.Domain.Enums;
using Chugim.Infrastructure.Persistence;
using Chugim.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static Chugim.Tests.TestSupport.TestData;

namespace Chugim.Tests.Infrastructure;

/// <summary>
/// ההוכחה שהמנגנון עובד מול מסד נתונים אמיתי: שני DbContext נפרדים קוראים את אותו חוג,
/// שניהם משנים אותו, הראשון שומר בהצלחה והשני נדחה.
/// משתמש בנתוני ה-seed: חוג 1 ("קליגרפיה עברית") — 3 מקומות, 2 רשומות (משתמשות 2, 3).
/// </summary>
public sealed class ConcurrencyTests : IDisposable
{
    private const int CourseId = 1;
    private const int Sara = 4;
    private const int Admin = 1;

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task TwoContextsTakeLastSpot_FirstSaves_SecondThrowsConcurrencyException()
    {
        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();

        // שתיהן קוראות את החוג באותו מצב: 2/3, אותו Version.
        var courseSeenByFirst = await first.Courses.SingleAsync(c => c.Id == CourseId);
        var courseSeenBySecond = await second.Courses.SingleAsync(c => c.Id == CourseId);

        // שתיהן רואות מקום פנוי ותופסות אותו בזיכרון.
        Assert.True(courseSeenByFirst.TryEnroll(Sara, Now, out _));
        Assert.True(courseSeenBySecond.TryEnroll(Admin, Now, out _));

        // הראשונה שומרת בהצלחה.
        await first.SaveChangesAsync();

        // השנייה נדחית: ה-Version שלה כבר לא תואם לשורה במסד.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        // ובמסד — בדיוק 3 רשומות ולא 4. ההרשמה של השנייה בוטלה יחד עם העדכון (אותה טרנזקציה).
        await using var verify = _database.CreateContext();
        var course = await verify.Courses.SingleAsync(c => c.Id == CourseId);
        var activeEnrollments = await verify.Enrollments
            .CountAsync(e => e.CourseId == CourseId && e.Status == EnrollmentStatus.Active);

        Assert.Equal(3, course.EnrolledCount);
        Assert.Equal(3, activeEnrollments);
    }

    [Fact]
    public async Task UnitOfWork_TranslatesEfConcurrencyException_ToApplicationException()
    {
        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();
        var secondUnitOfWork = new UnitOfWork(second);

        (await first.Courses.SingleAsync(c => c.Id == CourseId)).TryEnroll(Sara, Now, out _);
        (await second.Courses.SingleAsync(c => c.Id == CourseId)).TryEnroll(Admin, Now, out _);

        await first.SaveChangesAsync();

        // Application מכיר רק את ConcurrencyConflictException — לא את החריגה של EF.
        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None));
        Assert.IsType<DbUpdateConcurrencyException>(exception.InnerException);
    }

    [Fact]
    public async Task SaveChanges_RenewsVersionOfModifiedCourse()
    {
        await using var context = _database.CreateContext();
        var course = await context.Courses.SingleAsync(c => c.Id == CourseId);
        var versionBefore = course.Version;

        course.TryEnroll(Sara, Now, out _);
        await context.SaveChangesAsync();

        Assert.NotEqual(versionBefore, course.Version);
    }
}

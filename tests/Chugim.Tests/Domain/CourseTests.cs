using Chugim.Domain.Entities;
using Chugim.Domain.Enums;

namespace Chugim.Tests.Domain;

// בדיקות לחוקי המשאב המוגבל עצמם — בלי מסד נתונים ובלי Moq.
public class CourseTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    private static Course CreateCourse(int capacity, int enrolled = 0)
    {
        var course = new Course { Id = 1, Title = "קליגרפיה" };
        course.TryChangeCapacity(capacity);
        for (var userId = 1; userId <= enrolled; userId++)
            course.TryEnroll(userId, Now, out _);
        return course;
    }

    [Fact]
    public void TryEnroll_WhenSpotAvailable_TakesSpotAndCreatesEnrollment()
    {
        var course = CreateCourse(capacity: 10, enrolled: 9);

        var enrolled = course.TryEnroll(userId: 42, Now, out var enrollment);

        Assert.True(enrolled);
        Assert.Equal(10, course.EnrolledCount);
        Assert.True(course.IsFull);
        Assert.NotNull(enrollment);
        Assert.Equal(42, enrollment.UserId);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        Assert.Same(course, enrollment.Course);
        Assert.Contains(enrollment, course.Enrollments);
    }

    [Fact]
    public void TryEnroll_WhenFull_IsRejectedAndNothingChanges()
    {
        var course = CreateCourse(capacity: 10, enrolled: 10);

        var enrolled = course.TryEnroll(userId: 42, Now, out var enrollment);

        Assert.False(enrolled);
        Assert.Null(enrollment);
        Assert.Equal(10, course.EnrolledCount);
        Assert.Equal(10, course.Enrollments.Count);
    }

    [Fact]
    public void ReleaseSpot_FreesOneSpot()
    {
        var course = CreateCourse(capacity: 3, enrolled: 3);

        course.ReleaseSpot();

        Assert.Equal(2, course.EnrolledCount);
        Assert.Equal(1, course.AvailableSpots);
    }

    [Fact]
    public void ReleaseSpot_WhenNobodyEnrolled_Throws()
    {
        var course = CreateCourse(capacity: 3);

        Assert.Throws<InvalidOperationException>(course.ReleaseSpot);
    }

    [Theory]
    [InlineData(5, true)]   // מעל מספר הרשומות
    [InlineData(3, true)]   // בדיוק כמספר הרשומות
    [InlineData(2, false)]  // מתחת למספר הרשומות
    [InlineData(0, false)]  // מכסה לא חוקית
    public void TryChangeCapacity_RespectsEnrolledCount(int newCapacity, bool expected)
    {
        var course = CreateCourse(capacity: 10, enrolled: 3);

        var changed = course.TryChangeCapacity(newCapacity);

        Assert.Equal(expected, changed);
        Assert.Equal(expected ? newCapacity : 10, course.Capacity);
    }

    [Fact]
    public void TryCancel_ActiveEnrollment_CancelsAndReleasesSpot()
    {
        var course = CreateCourse(capacity: 3, enrolled: 2);
        course.TryEnroll(userId: 42, Now, out var enrollment);

        var cancelled = enrollment!.TryCancel();

        Assert.True(cancelled);
        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.Equal(2, course.EnrolledCount);
    }

    [Fact]
    public void TryCancel_AlreadyCancelled_DoesNotReleaseSpotTwice()
    {
        var course = CreateCourse(capacity: 3, enrolled: 2);
        course.TryEnroll(userId: 42, Now, out var enrollment);
        enrollment!.TryCancel();

        var cancelledAgain = enrollment.TryCancel();

        Assert.False(cancelledAgain);
        Assert.Equal(2, course.EnrolledCount);
    }
}

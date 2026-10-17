using Chugim.Application.Common;
using Chugim.Application.DTOs.Enrollments;
using Chugim.Application.Interfaces;
using Chugim.Application.Services;
using Chugim.Domain.Entities;
using Chugim.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using static Chugim.Tests.TestSupport.TestData;

namespace Chugim.Tests.Application;

/// <summary>
/// הלוגיקה של המשאב המוגבל בשכבת ה-service: המקרה התקין, המקרים שנדחים,
/// והטיפול בהתנגשות (ניסיון חוזר). ה-repositories וה-UnitOfWork מדומים ב-Moq.
/// </summary>
public class EnrollmentServiceTests
{
    private const int CourseId = 1;
    private static readonly CurrentUser Participant = new(42, UserRole.Participant);
    private static readonly EnrollRequest Request = new() { CourseId = CourseId };

    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<EnrollmentService>> _logger = new();
    private readonly EnrollmentService _service;

    public EnrollmentServiceTests()
    {
        _service = new EnrollmentService(
            _enrollments.Object, _courses.Object, _unitOfWork.Object, Mapper, _logger.Object);

        // אחרי שמירה מוצלחת ה-service טוען מחדש את ההרשמה (עם שם החוג והמשתתפת) לצורך התשובה.
        _enrollments
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Enrollment
            {
                CourseId = CourseId,
                UserId = Participant.Id,
                EnrolledAtUtc = Now,
                Course = CreateCourse(capacity: 10),
                User = new User { Id = Participant.Id, FullName = "שרה מזרחי" }
            });
    }

    // ---------- המקרה התקין ----------

    [Fact]
    public async Task EnrollAsync_WhenSpotAvailable_EnrollsAndSavesOnce()
    {
        var course = CourseReturnedFromDb(capacity: 10, enrolled: 9);

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CourseId, result.Value.CourseId);
        Assert.Equal(Participant.Id, result.Value.UserId);
        Assert.Equal(10, course.EnrolledCount);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- המקרים שנדחים ----------

    [Fact]
    public async Task EnrollAsync_WhenCourseFull_ReturnsConflictAndDoesNotSave()
    {
        CourseReturnedFromDb(capacity: 10, enrolled: 10);

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AppErrors.CourseFull, result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_WhenAlreadyEnrolled_ReturnsConflictAndDoesNotSave()
    {
        CourseReturnedFromDb(capacity: 10, enrolled: 5);
        _enrollments
            .Setup(r => r.HasActiveEnrollmentAsync(Participant.Id, CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.Equal(AppErrors.AlreadyEnrolled, result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_WhenCourseDoesNotExist_ReturnsNotFound()
    {
        _courses
            .Setup(r => r.GetForUpdateAsync(CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    // ---------- התנגשות על המשאב ----------

    [Fact]
    public async Task EnrollAsync_WhenConflictButSpotsRemain_RetriesWithFreshDataAndSucceeds()
    {
        // ניסיון 1: 5/10 — מישהי נרשמה בינתיים, השמירה נכשלת. ניסיון 2: קריאה טרייה, 6/10.
        _courses
            .SetupSequence(r => r.GetForUpdateAsync(CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCourse(capacity: 10, enrolled: 5))
            .ReturnsAsync(CreateCourse(capacity: 10, enrolled: 6));
        _unitOfWork
            .SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"))
            .Returns(Task.CompletedTask);

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _unitOfWork.Verify(u => u.ClearChanges(), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyConflictLogged(Times.Once());
    }

    [Fact]
    public async Task EnrollAsync_WhenConflictAndLastSpotTakenMeanwhile_ReturnsCourseFull()
    {
        // התרחיש מההגדרה: 9/10, שתיים לוחצות יחד. השנייה מתנגשת, קוראת שוב — 10/10.
        _courses
            .SetupSequence(r => r.GetForUpdateAsync(CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCourse(capacity: 10, enrolled: 9))
            .ReturnsAsync(CreateCourse(capacity: 10, enrolled: 10));
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"));

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.Equal(AppErrors.CourseFull, result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyConflictLogged(Times.Once());
    }

    [Fact]
    public async Task EnrollAsync_WhenConflictPersists_GivesUpAfterThreeAttempts()
    {
        _courses
            .Setup(r => r.GetForUpdateAsync(CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateCourse(capacity: 10, enrolled: 5)); // חוג "טרי" בכל קריאה
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"));

        var result = await _service.EnrollAsync(Participant, Request, CancellationToken.None);

        Assert.Equal(AppErrors.EnrollmentBusy, result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        VerifyConflictLogged(Times.Exactly(3));
    }

    // ---------- ביטול ----------

    [Fact]
    public async Task CancelAsync_ByOwner_CancelsAndReleasesSpot()
    {
        var course = CreateCourse(capacity: 3, enrolled: 2);
        course.TryEnroll(Participant.Id, Now, out var enrollment);
        SetupEnrollmentForUpdate(enrollment!);

        var result = await _service.CancelAsync(enrollment!.Id, Participant, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.Equal(2, course.EnrolledCount);
    }

    [Fact]
    public async Task CancelAsync_ByAnotherParticipant_ReturnsForbiddenAndChangesNothing()
    {
        var course = CreateCourse(capacity: 3, enrolled: 2);
        course.TryEnroll(Participant.Id, Now, out var enrollment);
        SetupEnrollmentForUpdate(enrollment!);
        var otherParticipant = new CurrentUser(99, UserRole.Participant);

        var result = await _service.CancelAsync(enrollment!.Id, otherParticipant, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- עזרים ----------

    private Course CourseReturnedFromDb(int capacity, int enrolled)
    {
        var course = CreateCourse(capacity, enrolled);
        _courses
            .Setup(r => r.GetForUpdateAsync(CourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);
        return course;
    }

    private void SetupEnrollmentForUpdate(Enrollment enrollment)
    {
        enrollment.User = new User { Id = enrollment.UserId, FullName = "רחל כהן" };
        _enrollments
            .Setup(r => r.GetForUpdateAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
    }

    private void VerifyConflictLogged(Times times) =>
        _logger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
}

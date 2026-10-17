using Chugim.Application.Common;
using Chugim.Application.DTOs.Courses;
using Chugim.Application.Interfaces;
using Chugim.Application.Services;
using Chugim.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Chugim.Tests.TestSupport.TestData;

namespace Chugim.Tests.Application;

public class CourseServiceTests
{
    private const int CourseId = 1;

    private readonly Mock<ICourseRepository> _courses = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _service = new CourseService(_courses.Object, _categories.Object, _unitOfWork.Object,
            Mapper, NullLogger<CourseService>.Instance);

        _categories.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Create_UnknownCategory_ReturnsValidationErrorAndDoesNotSave()
    {
        var result = await _service.CreateAsync(Request(categoryId: 99), CancellationToken.None);

        Assert.Equal(AppErrors.CategoryNotFound, result.Error);
        _courses.Verify(r => r.Add(It.IsAny<Course>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_CapacityBelowEnrolledCount_ReturnsConflictAndKeepsCourseUnchanged()
    {
        var course = ExistingCourse(capacity: 10, enrolled: 6);

        var result = await _service.UpdateAsync(CourseId, Request(capacity: 5), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal(10, course.Capacity);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenParticipantEnrolledMeanwhile_ReturnsConflict()
    {
        ExistingCourse(capacity: 10, enrolled: 6);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"));

        var result = await _service.UpdateAsync(CourseId, Request(capacity: 8), CancellationToken.None);

        Assert.Equal(AppErrors.CourseModifiedConcurrently, result.Error);
    }

    [Fact]
    public async Task Delete_CourseWithParticipants_ReturnsConflictAndDoesNotRemove()
    {
        ExistingCourse(capacity: 10, enrolled: 1);

        var result = await _service.DeleteAsync(CourseId, CancellationToken.None);

        Assert.Equal(AppErrors.CourseHasParticipants, result.Error);
        _courses.Verify(r => r.Remove(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task Delete_EmptyCourse_RemovesAndSaves()
    {
        var course = ExistingCourse(capacity: 10, enrolled: 0);

        var result = await _service.DeleteAsync(CourseId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _courses.Verify(r => r.Remove(course), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private Course ExistingCourse(int capacity, int enrolled)
    {
        var course = CreateCourse(capacity, enrolled, CourseId);
        course.CategoryId = 1;
        _courses.Setup(r => r.GetForUpdateAsync(CourseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        return course;
    }

    private static CourseRequest Request(int capacity = 10, int categoryId = 1) => new()
    {
        Title = "קליגרפיה עברית",
        Description = "כתיבה אומנותית",
        CategoryId = categoryId,
        InstructorName = "חנה ברגר",
        DayOfWeek = DayOfWeek.Sunday,
        StartTime = new TimeOnly(16, 30),
        StartDate = new DateOnly(2026, 11, 1),
        Location = "חדר 3",
        Price = 180m,
        Capacity = capacity
    };
}

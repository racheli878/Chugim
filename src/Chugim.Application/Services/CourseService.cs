using AutoMapper;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Courses;
using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Chugim.Application.Services;

public sealed class CourseService : ICourseService
{
    private readonly ICourseRepository _courses;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly ILogger<CourseService> _logger;

    public CourseService(
        ICourseRepository courses,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        TimeProvider time,
        ILogger<CourseService> logger)
    {
        _courses = courses;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _time = time;
        _logger = logger;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task<PagedResult<CourseResponse>> GetPagedAsync(CourseQuery query, CancellationToken cancellationToken)
    {
        var courses = await _courses.GetPagedAsync(query, cancellationToken);
        return courses.Map(c => _mapper.Map<CourseResponse>(c));
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var course = await _courses.GetByIdAsync(id, cancellationToken);
        if (course is null)
            return AppErrors.CourseNotFound;

        return _mapper.Map<CourseResponse>(course);
    }

    public async Task<Result<CourseResponse>> CreateAsync(CourseRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate < Today)
            return AppErrors.StartDateInPast;

        if (!await _categories.ExistsAsync(request.CategoryId, cancellationToken))
            return AppErrors.CategoryNotFound;

        var course = _mapper.Map<Course>(request);
        if (!course.TryChangeCapacity(request.Capacity))
            return AppErrors.InvalidCapacity;

        _courses.Add(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Course {CourseId} created", course.Id);

        // טעינה מחדש כדי לקבל גם את שם הקטגוריה לתשובה.
        var created = await _courses.GetByIdAsync(course.Id, cancellationToken);
        return _mapper.Map<CourseResponse>(created!);
    }

    public async Task<Result> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken)
    {
        var course = await _courses.GetForUpdateAsync(id, cancellationToken);
        if (course is null)
            return AppErrors.CourseNotFound;

        // חוג שכבר התחיל אפשר לערוך בלי לגעת בתאריך; רק תאריך *חדש* חייב להיות היום או בעתיד.
        if (request.StartDate != course.StartDate && request.StartDate < Today)
            return AppErrors.StartDateInPast;

        if (course.CategoryId != request.CategoryId &&
            !await _categories.ExistsAsync(request.CategoryId, cancellationToken))
            return AppErrors.CategoryNotFound;

        if (!course.TryChangeCapacity(request.Capacity))
            return Error.Conflict($"לא ניתן להקטין את מספר המקומות מתחת למספר הרשומות ({course.EnrolledCount}).");

        _mapper.Map(request, course);
        return await SaveCourseChangesAsync(course.Id, "updated", cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var course = await _courses.GetForUpdateAsync(id, cancellationToken);
        if (course is null)
            return AppErrors.CourseNotFound;

        if (course.EnrolledCount > 0)
            return AppErrors.CourseHasParticipants;

        _courses.Remove(course);
        return await SaveCourseChangesAsync(course.Id, "deleted", cancellationToken);
    }

    // שמירה משותפת לעדכון ולמחיקה: אם משתתפת נרשמה בין הקריאה לשמירה — 409.
    private async Task<Result> SaveCourseChangesAsync(int courseId, string action, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict: course {CourseId} was not {Action}", courseId, action);
            return AppErrors.CourseModifiedConcurrently;
        }

        _logger.LogInformation("Course {CourseId} {Action}", courseId, action);
        return Result.Success();
    }
}

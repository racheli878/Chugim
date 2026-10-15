using AutoMapper;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Enrollments;
using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Chugim.Application.Services;

public sealed class EnrollmentService : IEnrollmentService
{
    private const int MaxAttempts = 3;

    private readonly IEnrollmentRepository _enrollments;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        IEnrollmentRepository enrollments,
        ICourseRepository courses,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<EnrollmentService> logger)
    {
        _enrollments = enrollments;
        _courses = courses;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<Result<EnrollmentResponse>> EnrollAsync(
        CurrentUser user, EnrollRequest request, CancellationToken cancellationToken) =>
        RetryOnConflictAsync<Result<EnrollmentResponse>>(
            "enroll in course", request.CourseId,
            () => EnrollOnceAsync(user, request.CourseId, cancellationToken),
            AppErrors.EnrollmentBusy);

    public Task<Result> CancelAsync(int id, CurrentUser user, CancellationToken cancellationToken) =>
        RetryOnConflictAsync<Result>(
            "cancel enrollment", id,
            () => CancelOnceAsync(id, user, cancellationToken),
            AppErrors.CourseModifiedConcurrently);

    public async Task<PagedResult<EnrollmentResponse>> GetPagedAsync(
        CurrentUser user, PaginationQuery query, CancellationToken cancellationToken)
    {
        // מנהלת רואה הכול; משתתפת — רק את שלה.
        int? userFilter = user.IsAdmin ? null : user.Id;
        var enrollments = await _enrollments.GetPagedAsync(userFilter, null, query, cancellationToken);
        return enrollments.Map(e => _mapper.Map<EnrollmentResponse>(e));
    }

    public async Task<Result<PagedResult<EnrollmentResponse>>> GetByCourseAsync(
        int courseId, PaginationQuery query, CancellationToken cancellationToken)
    {
        if (!await _courses.ExistsAsync(courseId, cancellationToken))
            return AppErrors.CourseNotFound;

        var enrollments = await _enrollments.GetPagedAsync(null, courseId, query, cancellationToken);
        return enrollments.Map(e => _mapper.Map<EnrollmentResponse>(e));
    }

    public async Task<Result<EnrollmentResponse>> GetByIdAsync(
        int id, CurrentUser user, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollments.GetByIdAsync(id, cancellationToken);
        if (enrollment is null)
            return AppErrors.EnrollmentNotFound;

        if (!CanAccess(user, enrollment))
            return AppErrors.EnrollmentForbidden;

        return _mapper.Map<EnrollmentResponse>(enrollment);
    }

    // ניסיון אחד: קריאה טרייה, בדיקות, שינוי ושמירה.
    // אם מישהי שינתה את החוג בינתיים — SaveChangesAsync זורק ConcurrencyConflictException.
    private async Task<Result<EnrollmentResponse>> EnrollOnceAsync(
        CurrentUser user, int courseId, CancellationToken cancellationToken)
    {
        var course = await _courses.GetForUpdateAsync(courseId, cancellationToken);
        if (course is null)
            return AppErrors.CourseNotFound;

        if (await _enrollments.HasActiveEnrollmentAsync(user.Id, courseId, cancellationToken))
            return AppErrors.AlreadyEnrolled;

        if (!course.TryEnroll(user.Id, DateTime.UtcNow, out var enrollment))
        {
            _logger.LogInformation("Enrollment rejected: course {CourseId} is full", courseId);
            return AppErrors.CourseFull;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} enrolled in course {CourseId}", user.Id, courseId);

        // טעינה מחדש כדי לקבל את שם החוג ושם המשתתפת לתשובה.
        var created = await _enrollments.GetByIdAsync(enrollment.Id, cancellationToken);
        return _mapper.Map<EnrollmentResponse>(created!);
    }

    private async Task<Result> CancelOnceAsync(int id, CurrentUser user, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollments.GetForUpdateAsync(id, cancellationToken);
        if (enrollment is null)
            return AppErrors.EnrollmentNotFound;

        if (!CanAccess(user, enrollment))
            return AppErrors.EnrollmentForbidden;

        if (!enrollment.TryCancel())
            return AppErrors.EnrollmentAlreadyCancelled;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Enrollment {EnrollmentId} cancelled by user {UserId}", id, user.Id);
        return Result.Success();
    }

    /// <summary>
    /// מריץ את הניסיון עד MaxAttempts פעמים. בכל התנגשות: לוג Warning, ניקוי הנתונים הישנים
    /// מהזיכרון, וניסיון נוסף שקורא את המצב העדכני מהמסד.
    /// </summary>
    private async Task<TResult> RetryOnConflictAsync<TResult>(
        string operation, int entityId, Func<Task<TResult>> attempt, TResult whenExhausted)
    {
        for (var attemptNumber = 1; attemptNumber <= MaxAttempts; attemptNumber++)
        {
            try
            {
                return await attempt();
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning(
                    "Concurrency conflict on {Operation} {EntityId} (attempt {Attempt} of {MaxAttempts})",
                    operation, entityId, attemptNumber, MaxAttempts);
                _unitOfWork.ClearChanges();
            }
        }

        return whenExhausted;
    }

    private static bool CanAccess(CurrentUser user, Enrollment enrollment) =>
        user.IsAdmin || enrollment.UserId == user.Id;
}

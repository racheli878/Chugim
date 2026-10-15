using Chugim.Application.Common;
using Chugim.Application.DTOs.Enrollments;

namespace Chugim.Application.Services;

public interface IEnrollmentService
{
    Task<Result<EnrollmentResponse>> EnrollAsync(
        CurrentUser user, EnrollRequest request, CancellationToken cancellationToken);

    /// <summary>משתתפת: ההרשמות שלה. מנהלת: כל ההרשמות.</summary>
    Task<PagedResult<EnrollmentResponse>> GetPagedAsync(
        CurrentUser user, PaginationQuery query, CancellationToken cancellationToken);

    /// <summary>המשתתפות בחוג מסוים (מנהלת).</summary>
    Task<Result<PagedResult<EnrollmentResponse>>> GetByCourseAsync(
        int courseId, PaginationQuery query, CancellationToken cancellationToken);

    Task<Result<EnrollmentResponse>> GetByIdAsync(
        int id, CurrentUser user, CancellationToken cancellationToken);

    Task<Result> CancelAsync(int id, CurrentUser user, CancellationToken cancellationToken);
}

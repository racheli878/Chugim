using Chugim.Application.Common;
using Chugim.Application.DTOs.Courses;

namespace Chugim.Application.Services;

public interface ICourseService
{
    Task<PagedResult<CourseResponse>> GetPagedAsync(CourseQuery query, CancellationToken cancellationToken);
    Task<Result<CourseResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<CourseResponse>> CreateAsync(CourseRequest request, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

using Chugim.Application.Common;
using Chugim.Application.DTOs.Courses;
using Chugim.Domain.Entities;

namespace Chugim.Application.Interfaces;

public interface ICourseRepository
{
    /// <summary>קריאה בלבד: סינון, מיון ועימוד בתוך המסד, כולל הקטגוריה.</summary>
    Task<PagedResult<Course>> GetPagedAsync(CourseQuery query, CancellationToken cancellationToken);

    /// <summary>קריאה בלבד (AsNoTracking), כולל הקטגוריה.</summary>
    Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>ישות במעקב, לצורך שינוי ושמירה.</summary>
    Task<Course?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    void Add(Course course);
    void Remove(Course course);
}

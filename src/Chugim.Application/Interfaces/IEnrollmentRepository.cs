using Chugim.Application.Common;
using Chugim.Domain.Entities;

namespace Chugim.Application.Interfaces;

// אין Add: הרשמה נוצרת דרך Course.TryEnroll ונשמרת יחד עם החוג.
public interface IEnrollmentRepository
{
    /// <summary>קריאה בלבד, כולל החוג והמשתתפת. פילטר null = ללא סינון לפיו.</summary>
    Task<PagedResult<Enrollment>> GetPagedAsync(
        int? userId, int? courseId, PaginationQuery query, CancellationToken cancellationToken);

    /// <summary>קריאה בלבד, כולל החוג והמשתתפת.</summary>
    Task<Enrollment?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>ישות במעקב כולל החוג — נדרש לביטול (TryCancel משחרר מקום בחוג).</summary>
    Task<Enrollment?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> HasActiveEnrollmentAsync(int userId, int courseId, CancellationToken cancellationToken);
}

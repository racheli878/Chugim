using Chugim.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence;

public static class QueryableExtensions
{
    /// <summary>
    /// עימוד אמיתי מול המסד: COUNT אחד ו-SELECT אחד עם OFFSET/LIMIT.
    /// השאילתה חייבת להיות ממוינת לפני הקריאה, אחרת סדר העמודים לא מובטח.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, pagination.Page, pagination.PageSize, totalCount);
    }
}

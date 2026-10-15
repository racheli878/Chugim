using System.ComponentModel.DataAnnotations;

namespace Chugim.Application.Common;

/// <summary>
/// בסיס לכל בקשת רשימה. שאילתות עם סינון (למשל CourseQuery) יורשות ממנה.
/// </summary>
public class PaginationQuery
{
    public const int MaxPageSize = 50;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 10;

    public int Skip => (Page - 1) * PageSize;
}

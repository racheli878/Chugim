using System.ComponentModel.DataAnnotations;
using Chugim.Application.Common;

namespace Chugim.Application.DTOs.Courses;

// יורש Page ו-PageSize מ-PaginationQuery ומוסיף סינון ומיון.
public sealed class CourseQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    public bool? HasAvailableSpots { get; set; }

    [EnumDataType(typeof(CourseSortBy))]
    public CourseSortBy SortBy { get; set; } = CourseSortBy.StartDate;

    public bool Descending { get; set; }
}

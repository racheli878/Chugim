using System.ComponentModel.DataAnnotations;

namespace Chugim.Application.DTOs.Courses;

/// <summary>
/// משמש גם ליצירה (POST) וגם לעדכון (PUT) — אותם שדות ואותה ולידציה.
/// </summary>
public sealed record CourseRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Title { get; init; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string InstructorName { get; init; } = string.Empty;

    // nullable + Required: כך שדה שלא נשלח נדחה, במקום לקבל בשקט ערך ברירת מחדל.
    [Required, EnumDataType(typeof(DayOfWeek))]
    public DayOfWeek? DayOfWeek { get; init; }

    [Required]
    public TimeOnly? StartTime { get; init; }

    [Required]
    public DateOnly? StartDate { get; init; }

    [Required, StringLength(200)]
    public string Location { get; init; } = string.Empty;

    [Range(0, 10_000)]
    public decimal Price { get; init; }

    [Range(1, 500)]
    public int Capacity { get; init; }
}

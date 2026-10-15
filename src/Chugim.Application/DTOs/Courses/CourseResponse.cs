namespace Chugim.Application.DTOs.Courses;

public sealed record CourseResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string InstructorName { get; init; } = string.Empty;
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public DateOnly StartDate { get; init; }
    public string Location { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Capacity { get; init; }
    public int EnrolledCount { get; init; }
    public int AvailableSpots { get; init; }
}

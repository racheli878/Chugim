using Chugim.Domain.Enums;

namespace Chugim.Application.DTOs.Enrollments;

// משמש גם ל"ההרשמות שלי" וגם לרשימת המשתתפות בחוג (מנהלת).
public sealed record EnrollmentResponse
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string ParticipantName { get; init; } = string.Empty;
    public DateTime EnrolledAtUtc { get; init; }
    public EnrollmentStatus Status { get; init; }
}

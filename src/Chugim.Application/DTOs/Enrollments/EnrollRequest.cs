using System.ComponentModel.DataAnnotations;

namespace Chugim.Application.DTOs.Enrollments;

// המשתתפת עצמה נלקחת מה-token, לא מגוף הבקשה.
public sealed record EnrollRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }
}

using Chugim.Domain.Enums;

namespace Chugim.Domain.Entities;

// ישות הביניים של הקשר רבים-לרבים בין User ל-Course.
// נוצרת רק דרך Course.TryEnroll.
public class Enrollment
{
    public int Id { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
    public EnrollmentStatus Status { get; private set; } = EnrollmentStatus.Active;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive => Status == EnrollmentStatus.Active;

    /// <summary>
    /// מבטל ומשחרר את המקום בחוג. דורש ש-Course ייטען יחד עם ההרשמה.
    /// </summary>
    public bool TryCancel()
    {
        if (!IsActive)
            return false;

        Status = EnrollmentStatus.Cancelled;
        Course.ReleaseSpot();
        return true;
    }
}

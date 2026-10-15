using Chugim.Domain.Entities;
using Chugim.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence.Seed;

/// <summary>
/// נתוני התחלה (migration "SeedData"). הערכים קבועים — כולל ה-hash של הסיסמאות וה-Version —
/// כי HasData חייב להחזיר אותם ערכים בכל בנייה של המודל, אחרת נוצר migration חדש בכל פעם.
/// סיסמאות הדמו מתועדות ב-README: Admin123! (מנהלת), Demo1234! (משתתפות).
/// </summary>
internal static class SeedData
{
    private const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEB2H1qcwWTIgMrWnBxGcjJdltEiL9giwNQ+0pbkMa1WcBNnXcpcRSlOWaRaqt3ZDHQ==";
    private const string ParticipantPasswordHash =
        "AQAAAAIAAYagAAAAEM+VyVetoyFclqPM4dO1/O7ggNB/Ry926RGQrmVSdTOYAHemBfI6JD7zefuXL8R2CQ==";

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, FullName = "מנהלת החוגים", Email = "admin@chugim.dev", PasswordHash = AdminPasswordHash, Role = UserRole.Admin },
            new User { Id = 2, FullName = "רחל כהן", Email = "rachel@chugim.dev", PasswordHash = ParticipantPasswordHash, Role = UserRole.Participant },
            new User { Id = 3, FullName = "לאה לוי", Email = "leah@chugim.dev", PasswordHash = ParticipantPasswordHash, Role = UserRole.Participant },
            new User { Id = 4, FullName = "שרה מזרחי", Email = "sara@chugim.dev", PasswordHash = ParticipantPasswordHash, Role = UserRole.Participant });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "אומנות" },
            new Category { Id = 2, Name = "מחשבים" },
            new Category { Id = 3, Name = "בישול" },
            new Category { Id = 4, Name = "תפירה" });

        // אובייקטים אנונימיים: כך אפשר לקבוע גם שדות עם private set (Capacity, EnrolledCount, Version).
        modelBuilder.Entity<Course>().HasData(
            Course(1, "קליגרפיה עברית", "כתיבה אומנותית בעט ובמכחול.", 1, "חנה ברגר", DayOfWeek.Sunday, 16, 30, new DateOnly(2026, 11, 1), "חדר 3", 180m,
                capacity: 3, enrolled: 2, version: "6d1f0c52-3b8e-4f8a-9a52-1a7d3c9e4b01"), // כמעט מלא — להדגמת התחרות
            Course(2, "ציור בצבעי מים", "טכניקות בסיס, נופים וטבע דומם.", 1, "מרים שטרן", DayOfWeek.Tuesday, 17, 0, new DateOnly(2026, 11, 3), "חדר 3", 220m,
                capacity: 12, enrolled: 1, version: "2a7b9e14-5c3d-4e6f-8a91-0b2c4d6e8f02"),
            Course(3, "תכנות לנערות — צעדים ראשונים", "יסודות התכנות בסביבה ידידותית.", 2, "אסתר כץ", DayOfWeek.Monday, 15, 0, new DateOnly(2026, 11, 2), "מעבדת מחשבים", 250m,
                capacity: 15, enrolled: 0, version: "9c4e2f61-7a8b-4c3d-9e1f-2a3b4c5d6e03"),
            Course(4, "עיצוב גרפי", "עבודה עם כלי עיצוב והכנת הזמנות ופוסטרים.", 2, "דבורה פרידמן", DayOfWeek.Wednesday, 16, 0, new DateOnly(2026, 11, 4), "מעבדת מחשבים", 260m,
                capacity: 10, enrolled: 0, version: "4b8d1e73-9f2a-4b5c-8d6e-3c4d5e6f7a04"),
            Course(5, "אפייה ביתית", "לחמים, עוגות ועוגיות.", 3, "יהודית אורן", DayOfWeek.Thursday, 16, 30, new DateOnly(2026, 11, 5), "המטבח", 200m,
                capacity: 8, enrolled: 0, version: "7e2a5c84-1b3d-4e6f-9a8b-4d5e6f7a8b05"),
            Course(6, "תפירה למתחילות", "הכרת מכונת התפירה ותפירת פריטים פשוטים.", 4, "רבקה גולד", DayOfWeek.Sunday, 18, 0, new DateOnly(2026, 11, 8), "חדר 5", 230m,
                capacity: 10, enrolled: 0, version: "1f6c3d95-2e4a-4b7c-8d9e-5e6f7a8b9c06"));

        // ההרשמות תואמות ל-EnrolledCount שלמעלה (חוג 1: שתיים, חוג 2: אחת).
        var enrolledAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Enrollment>().HasData(
            new { Id = 1, CourseId = 1, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 2, CourseId = 1, UserId = 3, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 3, CourseId = 2, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active });
    }

    private static object Course(
        int id, string title, string description, int categoryId, string instructor,
        DayOfWeek day, int hour, int minute, DateOnly startDate, string location, decimal price,
        int capacity, int enrolled, string version) => new
    {
        Id = id,
        Title = title,
        Description = description,
        CategoryId = categoryId,
        InstructorName = instructor,
        DayOfWeek = day,
        StartTime = new TimeOnly(hour, minute),
        StartDate = startDate,
        Location = location,
        Price = price,
        Capacity = capacity,
        EnrolledCount = enrolled,
        Version = Guid.Parse(version)
    };
}

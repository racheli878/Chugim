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
                capacity: 10, enrolled: 0, version: "1f6c3d95-2e4a-4b7c-8d9e-5e6f7a8b9c06"),

            // migration "MoreDemoCourses": עוד חוגים כדי שיהיו שני עמודים, שני חוגים עם מקום אחד (לבדיקת 409)
            // וחוג גדול. מזהים מ-101 כדי לא להתנגש בחוגים שנוצרו ידנית במסד הפיתוח.
            Course(101, "סריגה ורקמה", "סריגה במסרגות, רקמת צלב ופריטים קטנים למתנה.", 4, "שולמית הלפרין", DayOfWeek.Monday, 17, 30, new DateOnly(2026, 11, 9), "חדר 5", 190m,
                capacity: 2, enrolled: 1, version: "a3c5e7f9-1b2d-4f6a-8c0e-2d4f6a8c0e11"), // מקום אחד — להדגמת 409
            Course(102, "פיסול בחימר", "עבודה בידיים, צורות בסיס וקלייה של פריט אישי.", 1, "נעמי רוזן", DayOfWeek.Wednesday, 15, 30, new DateOnly(2026, 11, 11), "חדר 3", 240m,
                capacity: 2, enrolled: 1, version: "b4d6f8a0-2c3e-4a7b-9d1f-3e5a7b9d1f12"), // מקום אחד — להדגמת 409
            Course(103, "קישוט עוגות", "קרם, בצק סוכר ועיצובים צבעוניים.", 3, "יהודית אורן", DayOfWeek.Tuesday, 16, 0, new DateOnly(2026, 11, 10), "המטבח", 210m,
                capacity: 6, enrolled: 0, version: "c5e7a9b1-3d4f-4b8c-8e2a-4f6b8c0e2a13"),
            Course(104, "הקלדה עיוורת", "הקלדה מהירה בעברית ובאנגלית, בלי להסתכל על המקלדת.", 2, "אסתר כץ", DayOfWeek.Thursday, 15, 0, new DateOnly(2026, 11, 12), "מעבדת מחשבים", 150m,
                capacity: 12, enrolled: 0, version: "d6f8b0c2-4e5a-4c9d-9f3b-5a7c9d1f3b14"),
            Course(105, "יצירה מחומרים ממוחזרים", "סדנה גדולה: הופכים קרטונים, בקבוקים ובדים ליצירות.", 1, "מלכה ויס", DayOfWeek.Friday, 10, 0, new DateOnly(2026, 11, 13), "האולם הגדול", 0m,
                capacity: 40, enrolled: 2, version: "e7a9c1d3-5f6b-4dae-8a4c-6b8d0e2a4c15"), // חוג גדול — פס מילוי במקום עיגולים
            Course(106, "תיקוני תפירה", "כפתורים, מכפלות ותיקונים קטנים שכל אחת צריכה לדעת.", 4, "רבקה גולד", DayOfWeek.Tuesday, 18, 30, new DateOnly(2026, 11, 17), "חדר 5", 120m,
                capacity: 8, enrolled: 0, version: "f8b0d2e4-6a7c-4ebf-9b5d-7c9e1f3b5d16"));

        // ההרשמות תואמות ל-EnrolledCount שלמעלה (חוג 1: שתיים, חוג 2: אחת).
        var enrolledAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Enrollment>().HasData(
            new { Id = 1, CourseId = 1, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 2, CourseId = 1, UserId = 3, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 3, CourseId = 2, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            // MoreDemoCourses — תואם ל-EnrolledCount של חוגים 101, 102, 105.
            new { Id = 101, CourseId = 101, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 102, CourseId = 102, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 103, CourseId = 105, UserId = 2, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active },
            new { Id = 104, CourseId = 105, UserId = 3, EnrolledAtUtc = enrolledAt, Status = EnrollmentStatus.Active });
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

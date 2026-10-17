using AutoMapper;
using Chugim.Application.Mappings;
using Chugim.Domain.Entities;

namespace Chugim.Tests.TestSupport;

// נתונים ועזרים משותפים לכל הבדיקות — כדי לא לשכפל את בניית החוג וה-mapper בכל קובץ.
public static class TestData
{
    public static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    // ה-mapper האמיתי עם ה-Profile של המערכת — בודקים גם שהמיפוי עובד, לא רק mock שלו.
    public static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    /// <summary>חוג עם מכסה נתונה ומספר משתתפות שכבר רשומות (משתמשות 1..enrolled).</summary>
    public static Course CreateCourse(int capacity, int enrolled = 0, int id = 1)
    {
        var course = new Course
        {
            Id = id,
            Title = "קליגרפיה עברית",
            Category = new Category { Id = 1, Name = "אומנות" }
        };
        course.TryChangeCapacity(capacity);

        for (var userId = 1; userId <= enrolled; userId++)
            course.TryEnroll(userId, Now, out _);

        return course;
    }
}

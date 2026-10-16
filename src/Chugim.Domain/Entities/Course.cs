using System.Diagnostics.CodeAnalysis;

namespace Chugim.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public DateOnly StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // המשאב המוגבל. שני השדות ניתנים לשינוי רק דרך המתודות שלמטה,
    // כך שאי אפשר לעקוף את בדיקת המקום הפנוי.
    public int Capacity { get; private set; }
    public int EnrolledCount { get; private set; }

    // Concurrency token. מוגדר כזה ומוחלף בכל שמירה בשכבת Infrastructure.
    public Guid Version { get; private set; } = Guid.NewGuid();

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public int AvailableSpots => Capacity - EnrolledCount;
    public bool IsFull => EnrolledCount >= Capacity;

    /// <summary>
    /// תופס מקום ויוצר הרשמה. המונה וההרשמה נוצרים תמיד יחד.
    /// </summary>
    public bool TryEnroll(int userId, DateTime enrolledAtUtc, [NotNullWhen(true)] out Enrollment? enrollment)
    {
        enrollment = null;
        if (IsFull)
            return false;

        EnrolledCount++;
        enrollment = new Enrollment { UserId = userId, EnrolledAtUtc = enrolledAtUtc, Course = this };
        Enrollments.Add(enrollment);
        return true;
    }

    public void ReleaseSpot()
    {
        if (EnrolledCount == 0)
            throw new InvalidOperationException($"Course {Id} has no enrolled participants to release.");

        EnrolledCount--;
    }

    /// <summary>
    /// משמש גם לקביעת המכסה ביצירת חוג. נכשל אם המכסה קטנה מ-1 או ממספר הרשומות.
    /// </summary>
    public bool TryChangeCapacity(int capacity)
    {
        if (capacity < 1 || capacity < EnrolledCount)
            return false;

        Capacity = capacity;
        return true;
    }
}

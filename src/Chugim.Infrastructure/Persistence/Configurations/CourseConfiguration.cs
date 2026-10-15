using Chugim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chugim.Infrastructure.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses", table =>
        {
            // רשת ביטחון ברמת המסד: גם באג בקוד לא יוכל לשמור חוג "מלא מדי".
            table.HasCheckConstraint("CK_Courses_Capacity", "\"Capacity\" >= 1");
            table.HasCheckConstraint("CK_Courses_EnrolledCount", "\"EnrolledCount\" >= 0 AND \"EnrolledCount\" <= \"Capacity\"");
        });

        builder.Property(c => c.Title).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.InstructorName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Location).IsRequired().HasMaxLength(200);
        builder.Property(c => c.DayOfWeek).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Price).HasPrecision(10, 2);

        // Concurrency token: מוחלף בכל שמירה ב-AppDbContext ונבדק בתנאי ה-WHERE.
        builder.Property(c => c.Version).IsConcurrencyToken();

        // שדות מחושבים — לא נשמרים כעמודות.
        builder.Ignore(c => c.AvailableSpots);
        builder.Ignore(c => c.IsFull);

        // one-to-many: קטגוריה → חוגים. אי אפשר למחוק קטגוריה שיש בה חוגים.
        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

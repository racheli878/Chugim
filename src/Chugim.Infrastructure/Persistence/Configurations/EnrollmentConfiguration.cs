using Chugim.Domain.Entities;
using Chugim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chugim.Infrastructure.Persistence.Configurations;

// Enrollment היא ישות הביניים של הקשר many-to-many בין User ל-Course,
// עם נתונים על הקשר עצמו (תאריך וסטטוס).
public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(e => e.IsActive);

        // מחיקת חוג (מותרת רק כשאין בו רשומות פעילות) מוחקת גם את ההרשמות המבוטלות שלו.
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // הרשמה פעילה אחת לכל משתתפת בכל חוג. הרשמות מבוטלות לא נספרות,
        // כך שאפשר להירשם שוב אחרי ביטול.
        builder.HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique()
            .HasFilter($"\"Status\" = '{EnrollmentStatus.Active}'");
    }
}

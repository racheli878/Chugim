using Chugim.Domain.Entities;
using Chugim.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // טוען את כל מחלקות IEntityTypeConfiguration מהפרויקט.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RenewCourseVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RenewCourseVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// נותן Version חדש לכל חוג ששונה. הערך הישן נשאר כ-OriginalValue, ו-EF מוסיף אותו
    /// לתנאי ה-WHERE של ה-UPDATE — כך שינוי שמבוסס על נתון שהתיישן לא ימצא שורה לעדכן.
    /// </summary>
    private void RenewCourseVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Course>())
        {
            if (entry.State == EntityState.Modified)
                entry.Property(c => c.Version).CurrentValue = Guid.NewGuid();
        }
    }
}

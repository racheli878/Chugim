using System.Linq.Expressions;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Courses;
using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence.Repositories;

public sealed class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _db;

    public CourseRepository(AppDbContext db) => _db = db;

    public async Task<PagedResult<Course>> GetPagedAsync(CourseQuery query, CancellationToken cancellationToken)
    {
        // כל ה-Where וה-OrderBy רק בונים את השאילתה. היא נשלחת למסד פעם אחת, ב-ToPagedResultAsync.
        var courses = _db.Courses.AsNoTracking().Include(c => c.Category).AsQueryable();

        if (query.CategoryId is int categoryId)
            courses = courses.Where(c => c.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            courses = courses.Where(c =>
                EF.Functions.Like(c.Title, pattern) || EF.Functions.Like(c.InstructorName, pattern));
        }

        // AvailableSpots לא קיים במסד, ולכן החישוב נכתב ישירות בשאילתה.
        if (query.HasAvailableSpots is bool hasSpots)
            courses = hasSpots
                ? courses.Where(c => c.EnrolledCount < c.Capacity)
                : courses.Where(c => c.EnrolledCount >= c.Capacity);

        return await ApplySorting(courses, query).ToPagedResultAsync(query, cancellationToken);
    }

    public Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _db.Courses.AsNoTracking()
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Course?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _db.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _db.Courses.AnyAsync(c => c.Id == id, cancellationToken);

    public void Add(Course course) => _db.Courses.Add(course);

    public void Remove(Course course) => _db.Courses.Remove(course);

    private static IQueryable<Course> ApplySorting(IQueryable<Course> courses, CourseQuery query)
    {
        var sorted = query.SortBy switch
        {
            CourseSortBy.Title => OrderBy(courses, c => c.Title, query.Descending),
            CourseSortBy.Price => OrderBy(courses, c => c.Price, query.Descending),
            CourseSortBy.AvailableSpots => OrderBy(courses, c => c.Capacity - c.EnrolledCount, query.Descending),
            _ => OrderBy(courses, c => c.StartDate, query.Descending)
        };

        // מיון משני לפי Id: מבטיח סדר יציב, כך שחוג לא יופיע בשני עמודים.
        return sorted.ThenBy(c => c.Id);
    }

    private static IOrderedQueryable<Course> OrderBy<TKey>(
        IQueryable<Course> courses, Expression<Func<Course, TKey>> key, bool descending) =>
        descending ? courses.OrderByDescending(key) : courses.OrderBy(key);
}

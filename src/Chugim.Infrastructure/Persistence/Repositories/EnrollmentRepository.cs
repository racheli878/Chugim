using Chugim.Application.Common;
using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Chugim.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence.Repositories;

public sealed class EnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _db;

    public EnrollmentRepository(AppDbContext db) => _db = db;

    public async Task<PagedResult<Enrollment>> GetPagedAsync(
        int? userId, int? courseId, PaginationQuery query, CancellationToken cancellationToken)
    {
        var enrollments = WithDetails(_db.Enrollments.AsNoTracking());

        if (userId is int u)
            enrollments = enrollments.Where(e => e.UserId == u);

        if (courseId is int c)
            enrollments = enrollments.Where(e => e.CourseId == c);

        return await enrollments
            .OrderByDescending(e => e.EnrolledAtUtc)
            .ThenByDescending(e => e.Id)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public Task<Enrollment?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        WithDetails(_db.Enrollments.AsNoTracking())
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Enrollment?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _db.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> HasActiveEnrollmentAsync(int userId, int courseId, CancellationToken cancellationToken) =>
        _db.Enrollments.AnyAsync(
            e => e.UserId == userId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active,
            cancellationToken);

    // כל מה שהתשובה צריכה נטען באותה שאילתה (JOIN) — בלי שאילתה נוספת לכל הרשמה (N+1).
    private static IQueryable<Enrollment> WithDetails(IQueryable<Enrollment> enrollments) =>
        enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
                .ThenInclude(c => c.Category);
}

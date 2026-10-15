using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(User user) => _db.Users.Add(user);
}

using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _db.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        _db.Categories.AnyAsync(c => c.Name == name, cancellationToken);

    public void Add(Category category) => _db.Categories.Add(category);
}

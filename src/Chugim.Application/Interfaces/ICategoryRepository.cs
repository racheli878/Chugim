using Chugim.Domain.Entities;

namespace Chugim.Application.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);
    void Add(Category category);
}

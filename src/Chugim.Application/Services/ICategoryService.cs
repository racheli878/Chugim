using Chugim.Application.DTOs.Categories;

namespace Chugim.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken);
}

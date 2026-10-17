using Chugim.Application.Common;
using Chugim.Application.DTOs.Categories;

namespace Chugim.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<CategoryResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<CategoryResponse>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken);
}

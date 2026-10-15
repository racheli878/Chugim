using AutoMapper;
using Chugim.Application.DTOs.Categories;
using Chugim.Application.Interfaces;

namespace Chugim.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository categories, IMapper mapper)
    {
        _categories = categories;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await _categories.GetAllAsync(cancellationToken);
        return _mapper.Map<List<CategoryResponse>>(categories);
    }
}

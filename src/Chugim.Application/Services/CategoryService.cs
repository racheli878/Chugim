using AutoMapper;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Categories;
using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Chugim.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categories, IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoryService> logger)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await _categories.GetAllAsync(cancellationToken);
        return _mapper.Map<List<CategoryResponse>>(categories);
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return AppErrors.CategoryDoesNotExist;

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<Result<CategoryResponse>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _categories.NameExistsAsync(name, cancellationToken))
            return AppErrors.CategoryNameTaken;

        var category = new Category { Name = name };
        _categories.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return _mapper.Map<CategoryResponse>(category);
    }
}

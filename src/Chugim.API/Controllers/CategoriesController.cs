using Chugim.Application.DTOs.Categories;
using Chugim.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chugim.API.Controllers;

[Route("api/categories")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _categoryService.GetAllAsync(cancellationToken));
}

using Chugim.API.Authorization;
using Chugim.Application.DTOs.Categories;
using Chugim.Application.Services;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken) =>
        FromResult(await _categoryService.GetByIdAsync(id, cancellationToken));

    /// <summary>הוספת קטגוריה (מנהלת בלבד).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CategoryRequest request, CancellationToken cancellationToken) =>
        CreatedFromResult(await _categoryService.CreateAsync(request, cancellationToken),
            nameof(GetById), category => new { id = category.Id });
}

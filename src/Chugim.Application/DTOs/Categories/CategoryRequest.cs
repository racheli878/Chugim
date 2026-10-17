using System.ComponentModel.DataAnnotations;

namespace Chugim.Application.DTOs.Categories;

public sealed record CategoryRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}

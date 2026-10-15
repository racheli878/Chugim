namespace Chugim.Application.DTOs.Categories;

public sealed record CategoryResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

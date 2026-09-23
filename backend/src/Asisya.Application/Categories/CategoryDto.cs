namespace Asisya.Application.Categories;

public sealed class CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string PhotoUrl { get; init; } = string.Empty;
}

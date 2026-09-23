namespace Asisya.Application.Categories;

public sealed class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
}

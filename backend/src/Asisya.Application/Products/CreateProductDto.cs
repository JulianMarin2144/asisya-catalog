namespace Asisya.Application.Products;

public sealed class CreateProductDto
{
    /// <summary>
    /// When greater than 1, triggers bulk random generation instead of single create.
    /// </summary>
    public int? Count { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
}

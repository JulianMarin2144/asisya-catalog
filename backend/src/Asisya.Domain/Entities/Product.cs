namespace Asisya.Domain.Entities;

public class Product
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    // Largest value that fits the numeric(18,2) price column.
    public const decimal MaxPrice = 9_999_999_999_999_999.99m;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Category Category { get; set; } = null!;
}

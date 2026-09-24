namespace Asisya.Domain.Entities;

public class Category
{
    public const int NameMaxLength = 128;
    public const int DescriptionMaxLength = 500;
    public const int PhotoUrlMaxLength = 512;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

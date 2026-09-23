namespace Asisya.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

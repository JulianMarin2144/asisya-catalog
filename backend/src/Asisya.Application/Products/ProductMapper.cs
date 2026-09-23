using Asisya.Domain.Entities;

namespace Asisya.Application.Products;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Stock = product.Stock,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty
    };

    public static ProductDetailDto ToDetailDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Stock = product.Stock,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        CategoryPhotoUrl = product.Category?.PhotoUrl ?? string.Empty
    };
}

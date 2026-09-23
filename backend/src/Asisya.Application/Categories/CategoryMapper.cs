using Asisya.Domain.Entities;

namespace Asisya.Application.Categories;

public static class CategoryMapper
{
    public static CategoryDto ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        PhotoUrl = category.PhotoUrl
    };
}

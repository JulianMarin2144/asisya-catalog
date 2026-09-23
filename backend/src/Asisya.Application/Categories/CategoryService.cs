using Asisya.Application.Abstractions;
using Asisya.Application.Common;
using Asisya.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Asisya.Application.Categories;

public sealed class CategoryService
{
    public const string ListCacheKey = "categories:all";
    private static readonly TimeSpan ListCacheDuration = TimeSpan.FromMinutes(5);

    private readonly ICategoryRepository _categories;
    private readonly IMemoryCache _cache;

    public CategoryService(ICategoryRepository categories, IMemoryCache cache)
    {
        _categories = categories;
        _cache = cache;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("Category name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            throw new BusinessException("Category photo URL is required.");
        }

        var name = request.Name.Trim();
        if (await _categories.ExistsByNameAsync(name, cancellationToken))
        {
            throw new BusinessException($"Category '{name}' already exists.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            PhotoUrl = request.PhotoUrl.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _categories.AddAsync(category, cancellationToken);
        await _categories.SaveChangesAsync(cancellationToken);
        _cache.Remove(ListCacheKey);

        return CategoryMapper.ToDto(category);
    }

    // Categories are read on every product form/filter and change rarely. The cache is per instance;
    // with several replicas a new category may take up to ListCacheDuration to appear on the others.
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(ListCacheKey, out IReadOnlyList<CategoryDto>? cached) && cached is not null)
        {
            return cached;
        }

        var categories = await _categories.GetAllAsync(cancellationToken);
        var items = categories.Select(CategoryMapper.ToDto).ToList();
        _cache.Set(ListCacheKey, (IReadOnlyList<CategoryDto>)items, ListCacheDuration);
        return items;
    }
}

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
        var name = request.Name?.Trim() ?? string.Empty;
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var photoUrl = request.PhotoUrl?.Trim() ?? string.Empty;

        Validate(name, description, photoUrl);

        if (await _categories.ExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Category '{name}' already exists.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            PhotoUrl = photoUrl,
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

    private static void Validate(string name, string? description, string photoUrl)
    {
        if (name.Length == 0)
        {
            throw new BusinessException("Category name is required.");
        }

        if (name.Length > Category.NameMaxLength)
        {
            throw new BusinessException($"Category name cannot exceed {Category.NameMaxLength} characters.");
        }

        if (description?.Length > Category.DescriptionMaxLength)
        {
            throw new BusinessException(
                $"Category description cannot exceed {Category.DescriptionMaxLength} characters.");
        }

        if (photoUrl.Length == 0)
        {
            throw new BusinessException("Category photo URL is required.");
        }

        if (photoUrl.Length > Category.PhotoUrlMaxLength)
        {
            throw new BusinessException($"Category photo URL cannot exceed {Category.PhotoUrlMaxLength} characters.");
        }

        if (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new BusinessException("Category photo URL must be an absolute http(s) URL.");
        }
    }
}

using System.Diagnostics;
using Asisya.Application.Abstractions;
using Asisya.Application.Common;
using Asisya.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Asisya.Application.Products;

public sealed class ProductService
{
    // 5k rows per SaveChanges keeps round-trips low while each batch stays well below
    // PostgreSQL's 65,535 bind-parameter limit (8 columns x 5k = 40k).
    public const int BulkBatchSize = 5000;
    public const int MaxBulkCount = 100_000;
    private static readonly string[] BulkCategoryNames = ["SERVIDORES", "CLOUD"];

    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        ILogger<ProductService> logger)
    {
        _products = products;
        _categories = categories;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> ListAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        var (items, total) = await _products.SearchAsync(page, pageSize, categoryId, search, cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = items.Select(ProductMapper.ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ProductDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdWithCategoryAsync(id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        return ProductMapper.ToDetailDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.Name, request.Price, request.Stock, request.CategoryId, cancellationToken);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _products.AddAsync(product, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);

        var created = await _products.GetByIdWithCategoryAsync(product.Id, cancellationToken);
        return ProductMapper.ToDto(created!);
    }

    public async Task<BulkGenerateResultDto> BulkGenerateAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 1)
        {
            throw new BusinessException("Bulk generation requires count greater than 1.");
        }

        if (count > MaxBulkCount)
        {
            throw new BusinessException($"Bulk generation cannot exceed {MaxBulkCount} products.");
        }

        var categories = await _categories.GetByNamesAsync(BulkCategoryNames, cancellationToken);
        var categoryIds = categories.Select(c => c.Id).ToArray();
        if (categoryIds.Length != BulkCategoryNames.Length)
        {
            var missing = BulkCategoryNames.Except(categories.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
            throw new BusinessException(
                $"Bulk generation requires categories SERVIDORES and CLOUD. Missing: {string.Join(", ", missing)}.");
        }

        var stopwatch = Stopwatch.StartNew();
        var inserted = 0;
        var batchCount = 0;
        var createdAt = DateTime.UtcNow;
        var random = Random.Shared;

        while (inserted < count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchSize = Math.Min(BulkBatchSize, count - inserted);
            var batch = new List<Product>(batchSize);

            for (var i = 0; i < batchSize; i++)
            {
                var seq = inserted + i + 1;
                batch.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    Name = $"Product-{seq:D6}-{random.Next(1000, 9999)}",
                    Description = $"Auto-generated product #{seq}",
                    Price = Math.Round((decimal)(random.NextDouble() * 9999) + 1m, 2),
                    Stock = random.Next(0, 1001),
                    CategoryId = categoryIds[random.Next(categoryIds.Length)],
                    CreatedAtUtc = createdAt
                });
            }

            await _products.AddRangeBatchAsync(batch, cancellationToken);
            inserted += batchSize;
            batchCount++;
        }

        stopwatch.Stop();

        var result = new BulkGenerateResultDto
        {
            RequestedCount = count,
            InsertedCount = inserted,
            BatchSize = BulkBatchSize,
            BatchCount = batchCount,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        };

        _logger.LogInformation(
            "Bulk product load finished: inserted={Inserted}, batches={Batches}, elapsedMs={ElapsedMs}",
            result.InsertedCount,
            result.BatchCount,
            result.ElapsedMilliseconds);

        return result;
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.Name, request.Price, request.Stock, request.CategoryId, cancellationToken);

        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _products.SaveChangesAsync(cancellationToken);

        var updated = await _products.GetByIdWithCategoryAsync(id, cancellationToken);
        return ProductMapper.ToDto(updated!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        _products.Remove(product);
        await _products.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(
        string name,
        decimal price,
        int stock,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessException("Product name is required.");
        }

        if (price <= 0)
        {
            throw new BusinessException("Product price must be greater than zero.");
        }

        if (stock < 0)
        {
            throw new BusinessException("Product stock cannot be negative.");
        }

        if (!await _categories.ExistsAsync(categoryId, cancellationToken))
        {
            throw new BusinessException($"Category '{categoryId}' does not exist.");
        }
    }
}

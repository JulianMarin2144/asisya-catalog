using Asisya.Domain.Entities;

namespace Asisya.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeBatchAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken = default);
    void Remove(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Asisya.Application.Abstractions;
using Asisya.Domain.Entities;
using Asisya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Asisya.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private const string LikeEscape = "\\";

    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Product?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking().Include(x => x.Category).AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern, LikeEscape));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await _db.Products.AddAsync(product, cancellationToken);

    // Detect-changes is disabled while adding (entities are new, nothing to diff) and the tracker is
    // cleared after each batch so memory stays flat across 100k inserts on the same scoped context.
    public async Task AddRangeBatchAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken = default)
    {
        var previousDetectChanges = _db.ChangeTracker.AutoDetectChangesEnabled;
        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            await _db.Products.AddRangeAsync(products, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            _db.ChangeTracker.Clear();
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = previousDetectChanges;
        }
    }

    public void Remove(Product product) => _db.Products.Remove(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private static string EscapeLikePattern(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape)
            .Replace("%", LikeEscape + "%")
            .Replace("_", LikeEscape + "_");
}

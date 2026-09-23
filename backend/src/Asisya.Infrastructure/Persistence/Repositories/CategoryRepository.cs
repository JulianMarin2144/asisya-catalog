using Asisya.Application.Abstractions;
using Asisya.Domain.Entities;
using Asisya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Asisya.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Categories.AnyAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _db.Categories.AsNoTracking().AnyAsync(x => x.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByNamesAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken = default)
    {
        var nameList = names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return await _db.Categories
            .AsNoTracking()
            .Where(x => nameList.Contains(x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await _db.Categories.AddAsync(category, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}

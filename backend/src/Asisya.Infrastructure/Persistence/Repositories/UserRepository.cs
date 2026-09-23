using Asisya.Application.Abstractions;
using Asisya.Domain.Entities;
using Asisya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Asisya.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }
}

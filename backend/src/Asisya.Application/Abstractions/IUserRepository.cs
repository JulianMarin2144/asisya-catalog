namespace Asisya.Application.Abstractions;

public interface IUserRepository
{
    Task<Domain.Entities.User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

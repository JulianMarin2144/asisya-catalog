using Asisya.Application.Auth;

namespace Asisya.Application.Abstractions;

public interface IJwtTokenService
{
    LoginResponse CreateToken(Domain.Entities.User user);
}

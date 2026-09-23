using Asisya.Application.Abstractions;
using Asisya.Application.Auth;

namespace Asisya.Application.Auth;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _users.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return _jwtTokenService.CreateToken(user);
    }
}

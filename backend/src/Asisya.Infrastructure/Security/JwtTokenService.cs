using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Asisya.Application.Abstractions;
using Asisya.Application.Auth;
using Asisya.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Asisya.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public LoginResponse CreateToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires
        };
    }
}

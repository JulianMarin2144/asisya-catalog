using System.Text;
using Asisya.Application.Abstractions;
using Asisya.Infrastructure.Persistence;
using Asisya.Infrastructure.Persistence.Repositories;
using Asisya.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Asisya.Infrastructure;

public static class DependencyInjection
{
    public const int MinJwtKeyBytes = 32;

    // Configuration is resolved from the service provider (not captured at registration time)
    // so hosts such as WebApplicationFactory can override it before the app is built.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Jwt:Issuer and Jwt:Audience are required.")
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= MinJwtKeyBytes,
                $"Jwt:Key must be at least {MinJwtKeyBytes} bytes (HS256).")
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(GetConnectionString(sp), npgsql => npgsql.CommandTimeout(300)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static string GetConnectionString(IServiceProvider services) =>
        services.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is missing.");
}

using Asisya.Application.Abstractions;
using Asisya.Domain.Entities;
using Asisya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Asisya.Infrastructure.Persistence;

public static class DbSeeder
{
    public const string DefaultUsername = "admin";
    public const string DefaultPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // With several replicas, set Database:InitializeOnStartup=false on the API and run
        // migrations + seed once as a deploy step, so instances don't race on schema changes.
        if (!configuration.GetValue("Database:InitializeOnStartup", true))
        {
            return;
        }

        await db.Database.MigrateAsync(cancellationToken);

        if (configuration.GetValue<bool>("Seed:DefaultAdmin")
            && !await db.Users.AnyAsync(x => x.Username == DefaultUsername, cancellationToken))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = DefaultUsername,
                PasswordHash = hasher.Hash(DefaultPassword),
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded default user '{Username}'.", DefaultUsername);
        }

        await EnsureCategoryAsync(db, "SERVIDORES", "Infraestructura on-prem",
            "https://cdn.example.com/categories/servidores.png", cancellationToken);
        await EnsureCategoryAsync(db, "CLOUD", "Servicios cloud",
            "https://cdn.example.com/categories/cloud.png", cancellationToken);
    }

    private static async Task EnsureCategoryAsync(
        AppDbContext db,
        string name,
        string description,
        string photoUrl,
        CancellationToken cancellationToken)
    {
        if (await db.Categories.AnyAsync(x => x.Name == name, cancellationToken))
        {
            return;
        }

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            PhotoUrl = photoUrl,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

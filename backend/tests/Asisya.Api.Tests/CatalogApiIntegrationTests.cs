using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Asisya.Application.Auth;
using Asisya.Application.Categories;
using Asisya.Application.Products;
using Asisya.Infrastructure.Persistence;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Asisya.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("asisya_test")
        .WithUsername("asisya")
        .WithPassword("asisya_secret")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public TestcontainersStates ContainerState => _postgres.State;

    public async Task InitializeAsync()
    {
        try
        {
            await _postgres.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Integration tests need a running Docker engine accessible to the current user (Testcontainers starts PostgreSQL). " +
                "Start Docker Desktop, or run only unit tests: dotnet test tests/Asisya.Application.Tests. Cause: " + ex.Message,
                ex);
        }
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "Asisya.Catalog",
                ["Jwt:Audience"] = "Asisya.Catalog.Clients",
                ["Jwt:Key"] = "AsisyaDevII_SuperSecret_Key_ChangeInProd_32+",
                ["Jwt:ExpirationMinutes"] = "120"
            });
        });
    }
}

public sealed class CatalogApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CatalogApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public void Api_UsesRunningPostgresContainer_NotInMemory()
    {
        Assert.Equal(TestcontainersStates.Running, _factory.ContainerState);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(db.Database.IsNpgsql());
        var expected = new NpgsqlConnectionStringBuilder(_factory.ConnectionString);
        var actual = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.Equal(expected.Port, actual.Port);
        Assert.Equal(expected.Database, actual.Database);
    }

    [Fact]
    public async Task EndToEnd_Login_Category_Product_Crud_Works()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var category = await PostAsync<CategoryDto>("/Category", new CreateCategoryDto
        {
            Name = $"CAT-{Guid.NewGuid():N}"[..12],
            Description = "Integration category",
            PhotoUrl = "https://cdn.example.com/categories/integration.png"
        });

        Assert.False(string.IsNullOrWhiteSpace(category.PhotoUrl));

        var created = await PostAsync<ProductDto>("/Product", new CreateProductDto
        {
            Name = "Integration Product",
            Description = "E2E",
            Price = 99.99m,
            Stock = 7,
            CategoryId = category.Id
        });

        var detailResponse = await _client.GetAsync($"/Products/{created.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<ProductDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(category.PhotoUrl, detail!.CategoryPhotoUrl);

        var updated = await PutAsync<ProductDto>($"/Product/{created.Id}", new UpdateProductDto
        {
            Name = "Integration Product Updated",
            Description = "Updated",
            Price = 120m,
            Stock = 3,
            CategoryId = category.Id
        });
        Assert.Equal("Integration Product Updated", updated.Name);
        Assert.Equal(3, updated.Stock);

        var deleteResponse = await _client.DeleteAsync($"/Product/{created.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missing = await _client.GetAsync($"/Products/{created.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/Category")]
    [InlineData("POST", "/Product")]
    public async Task CriticalWrites_WithoutToken_Return401(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/Products")]
    [InlineData("/Products/00000000-0000-0000-0000-000000000001")]
    [InlineData("/Category")]
    public async Task Reads_WithoutToken_Return401(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns409_AndInvalidInput_Returns400()
    {
        SetBearer(await LoginAsync());
        var body = new CreateCategoryDto
        {
            Name = $"DUP-{Guid.NewGuid():N}"[..12],
            PhotoUrl = "https://cdn.example.com/dup.png"
        };

        (await _client.PostAsJsonAsync("/Category", body)).EnsureSuccessStatusCode();
        var duplicate = await _client.PostAsJsonAsync("/Category", body);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, duplicate.StatusCode);

        var invalidUrl = await _client.PostAsJsonAsync("/Category",
            new CreateCategoryDto { Name = "BAD-URL", PhotoUrl = "not-a-url" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidUrl.StatusCode);

        var tooLong = await _client.PostAsJsonAsync("/Product", new CreateProductDto
        {
            Name = new string('A', 201),
            Price = 1,
            Stock = 1,
            CategoryId = Guid.NewGuid()
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task Migrations_CreateTrigramIndexForProductSearch()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT indexdef FROM pg_indexes WHERE tablename = 'products' AND indexname = 'IX_products_Name_trgm'",
            connection);

        var definition = (string?)await command.ExecuteScalarAsync();

        Assert.NotNull(definition);
        Assert.Contains("gin", definition);
        Assert.Contains("gin_trgm_ops", definition);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task ProductPutDelete_WithoutToken_Return401()
    {
        var id = Guid.NewGuid();

        var put = await _client.PutAsync($"/Product/{id}",
            new StringContent("""{"name":"X","price":1,"stock":1,"categoryId":"00000000-0000-0000-0000-000000000001"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, put.StatusCode);

        var delete = await _client.DeleteAsync($"/Product/{id}");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    [Fact]
    public async Task CriticalWrites_WithInvalidToken_Return401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        var category = await _client.PostAsync("/Category",
            new StringContent("""{"name":"X","photoUrl":"https://cdn.example.com/x.png"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, category.StatusCode);

        var product = await _client.PostAsync("/Product",
            new StringContent("""{"name":"X","price":1,"stock":1,"categoryId":"00000000-0000-0000-0000-000000000001"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, product.StatusCode);
    }

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "Admin123!"
        });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(login?.AccessToken));
        return login!.AccessToken;
    }

    private void SetBearer(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<T> PostAsync<T>(string path, object body)
    {
        var response = await _client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(payload);
        return payload!;
    }

    private async Task<T> PutAsync<T>(string path, object body)
    {
        var response = await _client.PutAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(payload);
        return payload!;
    }
}

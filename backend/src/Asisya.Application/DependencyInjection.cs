using Asisya.Application.Auth;
using Asisya.Application.Categories;
using Asisya.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Asisya.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<AuthService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<ProductService>();
        return services;
    }
}

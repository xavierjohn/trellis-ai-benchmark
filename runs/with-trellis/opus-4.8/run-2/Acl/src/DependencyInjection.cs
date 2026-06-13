namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;
using Trellis.Mediator;

/// <summary>Registers the anti-corruption layer (persistence + resource loaders).</summary>
public static class DependencyInjection
{
    /// <summary>Adds the anti-corruption layer to the container.</summary>
    public static IServiceCollection AddAntiCorruptionLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString)
                   .AddTrellisInterceptors());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<SharedResourceLoaderById<Order, OrderId>, OrderResourceLoader>();
        services.AddResourceAuthorization(
            typeof(CancelOrderCommand).Assembly,
            typeof(OrderResourceLoader).Assembly);
        services.AddTrellisUnitOfWork<AppDbContext>();

        return services;
    }
}

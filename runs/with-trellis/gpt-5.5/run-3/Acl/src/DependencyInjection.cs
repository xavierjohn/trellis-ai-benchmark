namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;
using Trellis.Mediator;

/// <summary>
/// Anti-corruption layer composition.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Register persistence adapters.</summary>
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

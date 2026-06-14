namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;
using Trellis.Mediator;

/// <summary>Dependency-injection wiring for the anti-corruption (persistence) layer.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the database context, repositories, and resource authorization.</summary>
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
            typeof(OrderManagement.Application.Orders.CancelOrderCommand).Assembly,
            typeof(OrderResourceLoader).Assembly);

        services.AddTrellisUnitOfWork<AppDbContext>();

        return services;
    }
}

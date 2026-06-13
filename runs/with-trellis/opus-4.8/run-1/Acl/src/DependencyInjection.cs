namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Orders;
using OrderManagement.AntiCorruptionLayer.Repositories;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;
using Trellis.Mediator;

/// <summary>Anti-corruption-layer (persistence) service registration.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the database context, repositories, resource loaders, and unit of work.</summary>
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

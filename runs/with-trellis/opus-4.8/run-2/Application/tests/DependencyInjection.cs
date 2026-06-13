namespace Application.Tests;

using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Mediator;
using Trellis.Testing;

public static class DependencyInjection
{
    public static IServiceCollection AddMockDependencies(this IServiceCollection services)
    {
        // Default actor is Admin (all permissions); individual tests scope a narrower actor.
        var actorProvider = new TestActorProvider("admin", Permissions.All.ToArray());
        services.AddSingleton(actorProvider);
        services.AddSingleton<IActorProvider>(actorProvider);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<FakeRepository<Customer, CustomerId>>();
        services.AddScoped<FakeRepository<Product, ProductId>>();
        services.AddScoped<FakeRepository<Order, OrderId>>();

        services.AddScoped<ICustomerRepository, FakeCustomerRepository>();
        services.AddScoped<IProductRepository, FakeProductRepository>();
        services.AddScoped<IOrderRepository, FakeOrderRepository>();

        services.AddScoped<SharedResourceLoaderById<Order, OrderId>, FakeOrderResourceLoader>();
        services.AddResourceAuthorization(typeof(CancelOrderCommand).Assembly, typeof(FakeOrderResourceLoader).Assembly);

        return services;
    }
}

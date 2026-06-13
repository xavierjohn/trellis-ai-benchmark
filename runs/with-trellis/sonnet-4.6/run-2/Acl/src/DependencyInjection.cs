namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Application.Todos;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;
using Trellis.Mediator;

public static class DependencyInjection
{
    public static IServiceCollection AddAntiCorruptionLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString)
                   .AddTrellisInterceptors());

        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddScoped<SharedResourceLoaderById<TodoItem, TodoId>, TodoItemResourceLoader>();
        services.AddScoped<SharedResourceLoaderById<Order, OrderId>, OrderResourceLoader>();

        services.AddResourceAuthorization(
            typeof(CancelOrderCommand).Assembly,
            typeof(OrderResourceLoader).Assembly);
        services.AddTrellisUnitOfWork<AppDbContext>();

        return services;
    }
}

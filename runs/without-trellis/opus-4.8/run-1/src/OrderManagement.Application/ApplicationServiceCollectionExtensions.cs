using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;

namespace OrderManagement.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();
        services.AddScoped<ProductService>();
        services.AddScoped<OrderService>();
        return services;
    }
}

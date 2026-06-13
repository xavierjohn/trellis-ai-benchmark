namespace OrderManagement.Application;

using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Customers;
using Trellis.Mediator;

/// <summary>Application-layer service registration.</summary>
public static class DependencyInjection
{
    /// <summary>Registers application services, mediator, and pipeline behaviors.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        services.AddDomainEventDispatch(typeof(CreateCustomerCommandHandler).Assembly);
        return services;
    }
}

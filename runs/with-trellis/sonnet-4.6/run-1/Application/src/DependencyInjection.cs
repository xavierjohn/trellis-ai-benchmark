namespace OrderManagement.Application;

using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Orders;
using Trellis.Mediator;

/// <summary>
/// Application layer dependency registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        services.AddDomainEventDispatch(typeof(CreateDraftOrderCommandHandler).Assembly);
        return services;
    }
}

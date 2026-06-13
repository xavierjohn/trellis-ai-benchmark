namespace OrderManagement.Application;

using Microsoft.Extensions.DependencyInjection;
using Trellis.Mediator;

/// <summary>Registers the application layer (mediator, behaviors, time provider).</summary>
public static class DependencyInjection
{
    /// <summary>Adds application services to the container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        services.AddDomainEventDispatch(typeof(DependencyInjection).Assembly);
        return services;
    }
}

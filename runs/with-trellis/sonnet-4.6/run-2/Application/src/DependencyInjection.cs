namespace OrderManagement.Application;

using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Customers;
using Trellis.Mediator;
using Trellis.Mediator.FluentValidation;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        services.AddDomainEventDispatch(typeof(CreateCustomerCommandHandler).Assembly);
        services.AddTrellisFluentValidation(typeof(CreateCustomerCommandHandler).Assembly);
        return services;
    }
}

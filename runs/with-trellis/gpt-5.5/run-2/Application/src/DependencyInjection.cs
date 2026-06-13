namespace OrderManagement.Application;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Application layer service registration.</summary>
public static class DependencyInjection
{
    /// <summary>Add application services.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}

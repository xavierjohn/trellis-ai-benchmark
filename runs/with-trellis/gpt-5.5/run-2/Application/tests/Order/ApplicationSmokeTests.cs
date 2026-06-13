namespace Application.Tests.Order;

using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application;

public sealed class ApplicationSmokeTests
{
    [Fact]
    public void AddApplication_registers_time_provider()
    {
        var services = new ServiceCollection().AddApplication().BuildServiceProvider();
        services.GetRequiredService<TimeProvider>().Should().Be(TimeProvider.System);
    }
}

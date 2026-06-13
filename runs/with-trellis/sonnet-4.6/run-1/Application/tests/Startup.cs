namespace Application.Tests;

using Microsoft.Extensions.Hosting;
using OrderManagement.Application;

public class Startup
{
    public static void ConfigureHost(IHostBuilder hostBuilder) =>
        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddApplication()
                .AddMockDependencies();
        });
}

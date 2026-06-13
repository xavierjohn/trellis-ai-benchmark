using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Api.Tests;

/// <summary>A TimeProvider whose current time can be set by tests.</summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now;
    public TestTimeProvider(DateTimeOffset start) => _now = start;
    public override DateTimeOffset GetUtcNow() => _now;
    public void SetUtcNow(DateTimeOffset value) => _now = value;
    public void Advance(TimeSpan delta) => _now += delta;
}

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    public TestTimeProvider Clock { get; }

    public ApiTestFactory(DateTimeOffset? start = null)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Clock = new TestTimeProvider(start ?? new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            RemoveAll(services, typeof(DbContextOptions<OrderManagementDbContext>));
            RemoveAll(services, typeof(OrderManagementDbContext));
            RemoveAll(services, typeof(TimeProvider));

            services.AddDbContext<OrderManagementDbContext>(options => options.UseSqlite(_connection));
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    private static void RemoveAll(IServiceCollection services, Type serviceType)
    {
        foreach (var d in services.Where(s => s.ServiceType == serviceType).ToList())
            services.Remove(d);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

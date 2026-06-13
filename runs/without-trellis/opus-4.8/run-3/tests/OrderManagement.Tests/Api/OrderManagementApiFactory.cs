using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.Api;

/// <summary>
/// Boots the real API in-process against a shared SQLite in-memory database and a
/// controllable <see cref="TimeProvider"/>.
/// </summary>
public sealed class OrderManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public TestTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development triggers EnsureCreated() in Program against our in-memory connection.
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            _connection.Open();

            services.RemoveAll<DbContextOptions<OrderManagementDbContext>>();
            services.RemoveAll<OrderManagementDbContext>();

            services.AddDbContext<OrderManagementDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

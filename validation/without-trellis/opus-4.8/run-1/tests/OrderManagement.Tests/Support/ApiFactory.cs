using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderManagement.Api.Infrastructure;

namespace OrderManagement.Tests.Support;

/// <summary>
/// Boots the real application with an isolated in-memory SQLite database and a controllable
/// <see cref="FakeTimeProvider"/> so integration tests get full HTTP round-trips.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public FakeTimeProvider Time { get; } =
        new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public ApiFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace the DbContext with the shared in-memory SQLite connection.
            services.RemoveAll<DbContextOptions<OrderManagementDbContext>>();
            services.RemoveAll<OrderManagementDbContext>();

            services.AddDbContext<OrderManagementDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace the system clock with a controllable one.
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

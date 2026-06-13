using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.Api;

/// <summary>
/// Test host using an in-memory SQLite database (shared open connection) and a controllable clock.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    public FakeTimeProvider Time { get; } =
        new(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderManagementDbContext>>();
            services.RemoveAll<OrderManagementDbContext>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<OrderManagementDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection?.Dispose();
    }
}

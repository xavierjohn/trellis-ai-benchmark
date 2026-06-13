using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Tests.Integration;

public class OrderManagementWebFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public OrderManagementWebFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production DbContext options
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrderManagementDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Register DbContext using the shared in-memory connection
            services.AddDbContext<OrderManagementDbContext>(options =>
                options.UseSqlite(_connection));
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure database schema is created after host is built
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

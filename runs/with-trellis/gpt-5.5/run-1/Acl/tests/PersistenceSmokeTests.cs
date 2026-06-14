namespace AntiCorruptionLayer.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.AntiCorruptionLayer;
using Trellis.EntityFrameworkCore;

public sealed class PersistenceSmokeTests
{
    [Fact]
    public async Task DbContext_creates_sqlite_schema()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddTrellisInterceptors()
            .Options;

        await using var context = new AppDbContext(options);

        (await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken)).Should().BeTrue();
        context.Model.FindEntityType(typeof(OrderManagement.Domain.Order)).Should().NotBeNull();
        context.Model.FindEntityType(typeof(OrderManagement.Domain.Product)).Should().NotBeNull();
        context.Model.FindEntityType(typeof(OrderManagement.Domain.Customer)).Should().NotBeNull();
    }
}

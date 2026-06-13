namespace AntiCorruptionLayer.Tests.Order;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.AntiCorruptionLayer;

public sealed class PersistenceSmokeTests
{
    [Fact]
    public async Task Database_model_can_be_created()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);

        (await db.Database.EnsureCreatedAsync()).Should().BeTrue();
    }
}

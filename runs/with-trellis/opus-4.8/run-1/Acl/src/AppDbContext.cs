namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Application database context with Trellis conventions.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Products.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Orders.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Constructor.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ApplyTrellisConventionsFor<AppDbContext>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

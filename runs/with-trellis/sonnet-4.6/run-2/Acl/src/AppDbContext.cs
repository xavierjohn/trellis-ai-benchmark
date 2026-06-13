namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Application database context with Trellis conventions.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Customer aggregate set.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Product aggregate set.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Order aggregate set.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Todo item aggregate set.</summary>
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    /// <summary>Creates a new AppDbContext.</summary>
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

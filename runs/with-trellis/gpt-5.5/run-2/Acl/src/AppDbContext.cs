namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Application database context with Trellis conventions.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ApplyTrellisConventionsFor<AppDbContext>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddTrellisInterceptors();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasIndex(customer => customer.Email).IsUnique();
            builder.OwnsOne(customer => customer.ShippingAddress);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasIndex(product => product.Sku).IsUnique();
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasIndex(order => order.CustomerId);
            builder.HasTrellisIndex(order => new { order.Status, order.SubmittedAt });
            builder.HasMany(order => order.LineItems)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(order => order.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}

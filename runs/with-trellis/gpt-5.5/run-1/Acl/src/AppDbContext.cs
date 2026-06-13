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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(customer => customer.Id);
            builder.Property(customer => customer.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(customer => customer.LastName).HasMaxLength(100).IsRequired();
            builder.Property(customer => customer.Email).HasMaxLength(320).IsRequired();
            builder.HasIndex(customer => customer.Email).IsUnique();
            builder.Property(customer => customer.PhoneNumber).HasMaxLength(32);
            builder.Property(customer => customer.Street).HasMaxLength(200).IsRequired();
            builder.Property(customer => customer.City).HasMaxLength(100).IsRequired();
            builder.Property(customer => customer.State).HasMaxLength(100).IsRequired();
            builder.Property(customer => customer.PostalCode).HasMaxLength(32).IsRequired();
            builder.Property(customer => customer.Country).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(product => product.Id);
            builder.Property(product => product.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(product => product.Sku).HasMaxLength(20).IsRequired();
            builder.HasIndex(product => product.Sku).IsUnique();
            builder.Property(product => product.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(order => order.Id);
            builder.Property(order => order.CreatedByActorId).HasMaxLength(200).IsRequired();
            builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            builder.Ignore(order => order.DomainEvents);
            builder.HasIndex(order => order.CustomerId);
            builder.HasIndex(order => new { order.Status, order.SubmittedAt });
            builder.HasMany(order => order.LineItems)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLineItem>(builder =>
        {
            builder.HasKey(lineItem => lineItem.Id);
            builder.Property(lineItem => lineItem.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(lineItem => lineItem.UnitPrice).HasPrecision(18, 2);
        });
    }
}

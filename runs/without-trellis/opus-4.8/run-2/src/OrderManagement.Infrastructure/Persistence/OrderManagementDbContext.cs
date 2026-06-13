using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrderManagementDbContext : DbContext
{
    public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot translate DateTimeOffset comparisons natively. The binary converter
        // stores values as an order-preserving long so range/ordering queries work.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            b.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            b.Property(c => c.Email).HasMaxLength(320).IsRequired();
            b.Property(c => c.PhoneNumber).HasMaxLength(40).IsRequired(false);
            b.HasIndex(c => c.Email).IsUnique();

            b.OwnsOne(c => c.ShippingAddress, a =>
            {
                a.Property(p => p.Street).HasColumnName("ShippingStreet").IsRequired();
                a.Property(p => p.City).HasColumnName("ShippingCity").IsRequired();
                a.Property(p => p.State).HasColumnName("ShippingState").IsRequired();
                a.Property(p => p.PostalCode).HasColumnName("ShippingPostalCode").IsRequired();
                a.Property(p => p.Country).HasColumnName("ShippingCountry").IsRequired();
            });
            b.Navigation(c => c.ShippingAddress).IsRequired();
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
            b.Property(p => p.Sku).HasMaxLength(20).IsRequired();
            b.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(p => p.StockQuantity);
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerId).IsRequired();
            b.Property(o => o.CreatedByActorId).IsRequired();
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(o => o.CreatedAt);
            b.Property(o => o.SubmittedAt);
            b.Property(o => o.ShippedAt);

            b.HasIndex(o => o.CustomerId);
            b.HasIndex(o => new { o.Status, o.SubmittedAt });

            b.Ignore(o => o.DomainEvents);
            b.Ignore(o => o.OrderTotal);

            var lineItems = b.Metadata.FindNavigation(nameof(Order.LineItems))!;
            lineItems.SetPropertyAccessMode(PropertyAccessMode.Field);

            b.HasMany(o => o.LineItems)
                .WithOne()
                .HasForeignKey(li => li.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LineItem>(b =>
        {
            b.HasKey(li => li.Id);
            b.Property(li => li.ProductId).IsRequired();
            b.Property(li => li.ProductName).HasMaxLength(200).IsRequired();
            b.Property(li => li.Quantity);
            b.Property(li => li.UnitPrice).HasColumnType("decimal(18,2)");
            b.Ignore(li => li.LineTotal);
        });
    }
}

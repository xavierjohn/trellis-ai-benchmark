using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Domain.Customers;
using OrderManagement.Api.Domain.Orders;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Infrastructure;

public sealed class OrderManagementDbContext : DbContext
{
    public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("Customers");
            b.HasKey(c => c.Id);
            b.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            b.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            b.Property(c => c.Email).IsRequired().HasMaxLength(320);
            b.HasIndex(c => c.Email).IsUnique();
            b.Property(c => c.PhoneNumber).IsRequired(false);

            b.OwnsOne(c => c.ShippingAddress, a =>
            {
                a.Property(p => p.Street).HasColumnName("Street").IsRequired();
                a.Property(p => p.City).HasColumnName("City").IsRequired();
                a.Property(p => p.State).HasColumnName("State").IsRequired();
                a.Property(p => p.PostalCode).HasColumnName("PostalCode").IsRequired();
                a.Property(p => p.Country).HasColumnName("Country").IsRequired();
            });
            b.Navigation(c => c.ShippingAddress).IsRequired();
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(p => p.Id);
            b.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
            b.Property(p => p.Sku).IsRequired().HasMaxLength(20);
            b.HasIndex(p => p.Sku).IsUnique();
            b.Property(p => p.UnitPrice).HasColumnType("TEXT").HasConversion<string>();
            b.Property(p => p.StockQuantity).IsRequired();
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerId).IsRequired();
            b.Property(o => o.CreatedByActorId).IsRequired();
            b.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            b.Property(o => o.CreatedAt).IsRequired();
            b.Property(o => o.SubmittedAt).IsRequired(false);
            b.Property(o => o.ShippedAt).IsRequired(false);

            b.HasIndex(o => o.CustomerId);
            b.HasIndex(o => new { o.Status, o.SubmittedAt });

            b.Ignore(o => o.Events);
            b.Ignore(o => o.OrderTotal);

            b.HasMany(o => o.LineItems)
                .WithOne()
                .HasForeignKey(li => li.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Metadata.FindNavigation(nameof(Order.LineItems))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(o => o.LineItems).AutoInclude();
        });

        modelBuilder.Entity<LineItem>(b =>
        {
            b.ToTable("LineItems");
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.UnitPrice).HasColumnType("TEXT").HasConversion<string>();
            b.Ignore(x => x.LineTotal);
            b.HasIndex(x => x.OrderId);
        });
    }
}

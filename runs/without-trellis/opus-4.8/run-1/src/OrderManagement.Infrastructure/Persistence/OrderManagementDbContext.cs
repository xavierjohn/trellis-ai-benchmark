using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrderManagementDbContext : DbContext, IUnitOfWork
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
            b.Property(c => c.Id).ValueGeneratedNever();
            b.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            b.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            b.Property(c => c.Email)
                .HasConversion(e => e.Value, v => Email.Create(v).Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(254);
            b.HasIndex(c => c.Email).IsUnique();
            b.Property(c => c.PhoneNumber).HasMaxLength(20); // nullable column
            b.OwnsOne(c => c.ShippingAddress, sa =>
            {
                sa.Property(p => p.Street).HasColumnName("Street").IsRequired();
                sa.Property(p => p.City).HasColumnName("City").IsRequired();
                sa.Property(p => p.State).HasColumnName("State").IsRequired();
                sa.Property(p => p.PostalCode).HasColumnName("PostalCode").IsRequired();
                sa.Property(p => p.Country).HasColumnName("Country").IsRequired();
            });
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).ValueGeneratedNever();
            b.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
            b.Property(p => p.Sku)
                .HasConversion(s => s.Value, v => Sku.Create(v).Value)
                .HasColumnName("Sku")
                .IsRequired()
                .HasMaxLength(20);
            b.HasIndex(p => p.Sku).IsUnique();
            b.Property(p => p.UnitPrice).HasPrecision(18, 2);
            b.Property(p => p.StockQuantity).IsRequired();
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.Id).ValueGeneratedNever();
            b.Property(o => o.CustomerId).IsRequired();
            b.Property(o => o.CreatedByActorId).IsRequired();
            b.Property(o => o.Status)
                .HasConversion<string>() // stored as string
                .IsRequired()
                .HasMaxLength(20);
            b.Property(o => o.CreatedAt).IsRequired();
            b.Property(o => o.SubmittedAt); // nullable
            b.Property(o => o.ShippedAt);   // nullable
            b.Ignore(o => o.DomainEvents);

            b.HasMany(o => o.LineItems)
                .WithOne()
                .HasForeignKey(li => li.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(o => o.LineItems)
                .HasField("_lineItems")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            b.HasIndex(o => o.CustomerId);
            b.HasIndex(o => new { o.Status, o.SubmittedAt }); // overdue query performance
        });

        modelBuilder.Entity<LineItem>(b =>
        {
            b.ToTable("LineItems");
            b.HasKey(li => li.Id);
            b.Property(li => li.Id).ValueGeneratedNever();
            b.Property(li => li.ProductId).IsRequired();
            b.Property(li => li.ProductName).IsRequired().HasMaxLength(200);
            b.Property(li => li.Quantity).IsRequired();
            b.Property(li => li.UnitPrice).HasPrecision(18, 2);
        });
    }
}
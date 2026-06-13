using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Domain;

namespace OrderManagement.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<LineItem> LineItems => Set<LineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(256);
            entity.Property(c => c.PhoneNumber).HasMaxLength(50);

            entity.OwnsOne(c => c.ShippingAddress, addr =>
            {
                addr.Property(a => a.Street).IsRequired().HasColumnName("Street");
                addr.Property(a => a.City).IsRequired().HasColumnName("City");
                addr.Property(a => a.State).IsRequired().HasColumnName("State");
                addr.Property(a => a.PostalCode).IsRequired().HasColumnName("PostalCode");
                addr.Property(a => a.Country).IsRequired().HasColumnName("Country");
            });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(p => p.SKU).IsRequired().HasMaxLength(20);
            entity.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => new { o.Status, o.SubmittedAt });
            entity.Property(o => o.Status).HasConversion<string>().IsRequired();
            entity.Property(o => o.CreatedByActorId).IsRequired();

            entity.HasMany(o => o.LineItems)
                .WithOne()
                .HasForeignKey(li => li.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LineItem>(entity =>
        {
            entity.HasKey(li => li.Id);
            entity.Property(li => li.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(li => li.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}

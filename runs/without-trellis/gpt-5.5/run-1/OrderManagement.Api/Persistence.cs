using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderManagement.Api;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.PhoneNumber).HasMaxLength(32);
        builder.OwnsOne(c => c.ShippingAddress, address =>
        {
            address.Property(a => a.Street).HasMaxLength(200).IsRequired();
            address.Property(a => a.City).HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasMaxLength(100).IsRequired();
            address.Property(a => a.PostalCode).HasMaxLength(32).IsRequired();
            address.Property(a => a.Country).HasMaxLength(100).IsRequired();
        });
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.StockQuantity).IsRequired();
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.CreatedByActorId).HasMaxLength(128).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(o => o.CreatedAt).HasConversion(v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        builder.Property(o => o.SubmittedAt).HasConversion(v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null, v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        builder.Property(o => o.ShippedAt).HasConversion(v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null, v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.SubmittedAt });
        builder.Ignore(o => o.Total);

        builder.HasMany(o => o.LineItems)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

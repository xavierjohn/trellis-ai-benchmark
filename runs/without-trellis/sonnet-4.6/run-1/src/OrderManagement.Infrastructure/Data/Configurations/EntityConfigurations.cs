using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Aggregates;

namespace OrderManagement.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(320);
        builder.Property(c => c.PhoneNumber).HasMaxLength(50);

        builder.HasIndex(c => c.Email).IsUnique();

        builder.OwnsOne(c => c.ShippingAddress, sa =>
        {
            sa.Property(a => a.Street).IsRequired().HasColumnName("Street").HasMaxLength(500);
            sa.Property(a => a.City).IsRequired().HasColumnName("City").HasMaxLength(200);
            sa.Property(a => a.State).IsRequired().HasColumnName("State").HasMaxLength(200);
            sa.Property(a => a.PostalCode).IsRequired().HasColumnName("PostalCode").HasMaxLength(20);
            sa.Property(a => a.Country).IsRequired().HasColumnName("Country").HasMaxLength(100);
        });
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(20);
        builder.Property(p => p.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.StockQuantity).IsRequired();

        builder.HasIndex(p => p.SKU).IsUnique();
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.CreatedByActorId).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.SubmittedAt);
        builder.Property(o => o.ShippedAt);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.SubmittedAt });

        builder.HasMany(o => o.LineItems)
            .WithOne()
            .HasForeignKey(li => li.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.LineItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_lineItems");
    }
}

public class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    public void Configure(EntityTypeBuilder<LineItem> builder)
    {
        builder.HasKey(li => li.Id);

        builder.Property(li => li.ProductId).IsRequired();
        builder.Property(li => li.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(li => li.Quantity).IsRequired();
        builder.Property(li => li.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
    }
}

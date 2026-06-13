using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.CreatedByActorId).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.SubmittedAt).IsRequired(false);
        builder.Property(o => o.ShippedAt).IsRequired(false);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.SubmittedAt });

        builder.HasMany(o => o.LineItems)
            .WithOne()
            .HasForeignKey(li => li.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.LineItems)
            .HasField("_lineItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.OrderTotal);
    }
}

public sealed class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    public void Configure(EntityTypeBuilder<LineItem> builder)
    {
        builder.ToTable("LineItems");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.OrderId).IsRequired();
        builder.Property(li => li.ProductId).IsRequired();
        builder.Property(li => li.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(li => li.Quantity).IsRequired();
        builder.Property(li => li.UnitPrice).HasColumnType("TEXT").IsRequired();

        builder.Ignore(li => li.LineTotal);
    }
}

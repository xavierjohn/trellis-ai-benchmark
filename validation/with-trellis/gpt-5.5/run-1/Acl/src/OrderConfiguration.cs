namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>Order EF configuration.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.CreatedByActorId).IsRequired();
        builder.Property(order => order.Status).IsRequired();
        builder.HasIndex(order => order.CustomerId);
        builder.HasTrellisIndex(order => new { order.Status, order.SubmittedAt });

        builder.HasMany(order => order.LineItems)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Order line item EF configuration.</summary>
internal sealed class OrderLineItemConfiguration : IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<OrderLineItem> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductName).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.UnitPrice).IsRequired();
    }
}

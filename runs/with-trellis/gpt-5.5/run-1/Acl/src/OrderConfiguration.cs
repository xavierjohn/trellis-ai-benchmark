namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core configuration for orders and line items.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>, IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.CreatedByActorId).IsRequired().HasMaxLength(200);
        builder.Property(order => order.Status).IsRequired();
        builder.Ignore(order => order.LineItems);
        builder.HasTrellisIndex(order => order.CustomerId);
        builder.HasTrellisIndex(order => new { order.Status, order.SubmittedAt });
        builder.HasMany<OrderLineItem>("_lineItems")
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_lineItems").UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    public void Configure(EntityTypeBuilder<OrderLineItem> builder)
    {
        builder.HasKey(line => line.Id);
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.ProductName).IsRequired();
        builder.Property(line => line.Quantity).IsRequired();
        builder.Property(line => line.UnitPrice).IsRequired();
    }
}

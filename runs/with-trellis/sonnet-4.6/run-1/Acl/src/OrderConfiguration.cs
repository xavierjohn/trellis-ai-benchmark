namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF configuration for <see cref="Order"/>.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Ignore(order => order.LineItems);
        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.CreatedByActorId).IsRequired();
        builder.Property(order => order.Status).IsRequired();
        builder.HasTrellisIndex(order => new { order.CustomerId, order.Status, order.SubmittedAt });

        builder.OwnsMany<LineItem>("_lineItems", lineItems =>
        {
            lineItems.ToTable("LineItems");
            lineItems.WithOwner().HasForeignKey("OrderId");
            lineItems.HasKey(lineItem => lineItem.Id);
            lineItems.Property(lineItem => lineItem.ProductId).IsRequired();
            lineItems.Property(lineItem => lineItem.ProductName).IsRequired().HasMaxLength(200);
            lineItems.Property(lineItem => lineItem.Quantity).IsRequired();
            lineItems.Property(lineItem => lineItem.UnitPrice).IsRequired();
        });

        builder.Metadata.FindNavigation("_lineItems")?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

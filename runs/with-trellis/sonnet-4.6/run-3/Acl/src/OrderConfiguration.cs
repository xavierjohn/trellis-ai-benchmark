namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

internal class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.CreatedByActorId).IsRequired().HasMaxLength(200);

        builder.OwnsMany(order => order.LineItems, lineItem =>
        {
            lineItem.WithOwner().HasForeignKey("OrderId");
            lineItem.HasKey(item => item.Id);
            lineItem.Property(item => item.Quantity).IsRequired();
        });

        builder.Navigation(order => order.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasTrellisIndex(order => new { order.CustomerId, order.CreatedAt });
        builder.HasTrellisIndex(order => new { order.Status, order.SubmittedAt });
    }
}

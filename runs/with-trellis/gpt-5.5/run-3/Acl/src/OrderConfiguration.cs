namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasTrellisIndex(o => o.CustomerId);
        builder.HasTrellisIndex(o => new { o.Status, o.SubmittedAt });
        builder.Ignore(o => o.LineItems);

        builder.OwnsMany<LineItem>("_lineItems", line =>
        {
            line.ToTable("OrderLineItems");
            line.WithOwner().HasForeignKey("OrderId");
            line.HasKey(l => l.Id);
        });

        builder.Navigation("_lineItems").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core configuration for the Order aggregate.</summary>
internal class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.CreatedByActorId).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Status).IsRequired();

        builder.HasIndex(o => o.CustomerId);
        builder.HasTrellisIndex(o => new { o.Status, o.SubmittedAt });

        builder.OwnsMany(o => o.LineItems, liNav =>
        {
            liNav.WithOwner().HasForeignKey("OrderId");
            liNav.HasKey(li => li.Id);
            liNav.ToTable("LineItems");
            liNav.Property(li => li.ProductId).IsRequired();
            liNav.Property(li => li.ProductName).IsRequired();
            liNav.Property(li => li.Quantity).IsRequired();
            liNav.Property(li => li.UnitPrice).IsRequired();
        });
        builder.Navigation(o => o.LineItems)
            .HasField("_lineItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core configuration for the <see cref="Order"/> aggregate.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.CreatedByActorId).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Status).IsRequired();

        // Owned line-item collection mapped against the private backing field.
        builder.Ignore(o => o.LineItems);
        builder.OwnsMany<LineItem>("_lineItems", li =>
        {
            li.ToTable("LineItems");
            li.HasKey(x => x.Id);
        });

        // Index for "orders by customer" lookups.
        builder.HasIndex(o => o.CustomerId);

        // Index for the overdue query (Status + SubmittedAt). SubmittedAt is a Maybe<DateTime>.
        builder.HasTrellisIndex(o => new { o.Status, o.SubmittedAt });
    }
}

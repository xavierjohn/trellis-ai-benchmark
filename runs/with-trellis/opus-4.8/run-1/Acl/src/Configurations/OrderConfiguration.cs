namespace OrderManagement.AntiCorruptionLayer.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core configuration for the <see cref="Order"/> aggregate.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        // Computed, read-only properties are not persisted.
        builder.Ignore(o => o.OrderTotal);
        builder.Ignore(o => o.ReservesStock);

        // The public facade is IReadOnlyList<T> — EF cannot materialize an interface.
        // Ignore the facade and map directly against the private backing field by name.
        builder.Ignore(o => o.LineItems);
        builder.OwnsMany<LineItem>("_lineItems", li =>
        {
            li.ToTable("LineItems");
            li.HasKey(x => x.Id);
            li.Ignore(x => x.LineTotal);
        });

        builder.HasIndex(o => o.CustomerId);
        builder.HasTrellisIndex(o => new { o.Status, o.SubmittedAt });
    }
}

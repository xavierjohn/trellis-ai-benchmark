namespace OrderManagement.AntiCorruptionLayer;

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
        builder.HasIndex(o => o.CustomerId);
        builder.HasTrellisIndex(o => new { o.Status, o.SubmittedAt });

        // The public LineItems facade is IReadOnlyList<T> — EF cannot instantiate an interface.
        // Map directly against the private backing field by name.
        builder.Ignore(o => o.LineItems);
        builder.OwnsMany<LineItem>("_lineItems", li =>
        {
            li.ToTable("LineItems");
            li.HasKey(x => x.Id);
        });
    }
}

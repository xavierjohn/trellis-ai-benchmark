namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>EF Core configuration for the <see cref="Product"/> aggregate.</summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Sku).IsUnique();
        // UnitPrice (MonetaryAmount scalar VO) is mapped automatically by the Trellis conventions.
    }
}

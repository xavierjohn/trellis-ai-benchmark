namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>
/// EF configuration for <see cref="Product"/>.
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).IsRequired();
        builder.Property(product => product.Sku).IsRequired();
        builder.Property(product => product.UnitPrice).IsRequired();
        builder.Property(product => product.StockQuantity).IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();
    }
}

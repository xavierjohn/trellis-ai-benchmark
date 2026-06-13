using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Products;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(20);
        builder.Property(p => p.UnitPrice).HasColumnType("TEXT").IsRequired();
        builder.Property(p => p.StockQuantity).IsRequired();

        builder.HasIndex(p => p.Sku).IsUnique();
    }
}

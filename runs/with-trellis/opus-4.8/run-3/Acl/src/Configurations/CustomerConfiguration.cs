namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>EF Core configuration for the <see cref="Customer"/> aggregate.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired();

        // Email is unique across all customers.
        builder.HasIndex(c => c.Email).IsUnique();

        // ShippingAddress (composite VO) is auto-mapped as an owned type by the convention.
    }
}

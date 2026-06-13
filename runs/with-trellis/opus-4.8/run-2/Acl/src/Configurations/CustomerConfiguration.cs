namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>EF Core configuration for the <see cref="Customer"/> aggregate.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Email).IsUnique();
        // ShippingAddress (composite VO) and PhoneNumber (Maybe<PhoneNumber>) are mapped
        // automatically by the Trellis conventions — no explicit configuration needed.
    }
}

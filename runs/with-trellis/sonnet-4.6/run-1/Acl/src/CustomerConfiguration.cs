namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>
/// EF configuration for <see cref="Customer"/>.
/// </summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.FirstName).IsRequired();
        builder.Property(customer => customer.LastName).IsRequired();
        builder.Property(customer => customer.Email).IsRequired();
        builder.HasIndex(customer => customer.Email).IsUnique();
    }
}

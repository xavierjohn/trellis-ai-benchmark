namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>
/// EF Core configuration for customers.
/// </summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.FirstName).IsRequired();
        builder.Property(customer => customer.LastName).IsRequired();
        builder.Property(customer => customer.Email).IsRequired();
        builder.HasIndex(customer => customer.Email).IsUnique();
    }
}

namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain;

/// <summary>Customer EF configuration.</summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.FirstName).IsRequired();
        builder.Property(customer => customer.LastName).IsRequired();
        builder.Property(customer => customer.Email).IsRequired();
        builder.OwnsOne(customer => customer.ShippingAddress, address =>
        {
            address.Property(a => a.Street).IsRequired();
            address.Property(a => a.City).IsRequired();
            address.Property(a => a.State).IsRequired();
            address.Property(a => a.PostalCode).IsRequired();
            address.Property(a => a.Country).IsRequired();
        });
        builder.HasIndex(customer => customer.Email).IsUnique();
    }
}

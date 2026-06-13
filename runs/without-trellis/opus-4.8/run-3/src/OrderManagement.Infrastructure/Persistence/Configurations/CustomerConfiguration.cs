using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(320);
        builder.Property(c => c.PhoneNumber).HasMaxLength(40).IsRequired(false);

        builder.HasIndex(c => c.Email).IsUnique();

        builder.OwnsOne(c => c.ShippingAddress, address =>
        {
            address.Property(a => a.Street).IsRequired().HasColumnName("ShippingStreet");
            address.Property(a => a.City).IsRequired().HasColumnName("ShippingCity");
            address.Property(a => a.State).IsRequired().HasColumnName("ShippingState");
            address.Property(a => a.PostalCode).IsRequired().HasColumnName("ShippingPostalCode");
            address.Property(a => a.Country).IsRequired().HasColumnName("ShippingCountry");
        });

        builder.Navigation(c => c.ShippingAddress).IsRequired();
    }
}

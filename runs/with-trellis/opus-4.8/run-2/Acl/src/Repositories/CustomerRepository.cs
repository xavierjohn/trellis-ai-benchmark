namespace OrderManagement.AntiCorruptionLayer;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core repository for the <see cref="Customer"/> aggregate.</summary>
public sealed class CustomerRepository(AppDbContext context)
    : RepositoryBase<Customer, CustomerId>(context), ICustomerRepository;

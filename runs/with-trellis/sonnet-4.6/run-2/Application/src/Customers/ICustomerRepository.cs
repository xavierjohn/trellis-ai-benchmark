namespace OrderManagement.Application.Customers;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Repository for customer aggregates.</summary>
public interface ICustomerRepository
{
    /// <summary>Finds a customer by their unique identifier.</summary>
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>Finds a customer by their email address.</summary>
    Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>Adds a new customer to the repository.</summary>
    void Add(Customer customer);
}

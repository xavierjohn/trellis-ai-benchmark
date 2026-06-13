namespace OrderManagement.Application;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>
/// Customer repository contract.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>Finds a customer by identifier.</summary>
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>Finds a customer by email address.</summary>
    Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>Stages a customer for insertion.</summary>
    void Add(Customer customer);
}

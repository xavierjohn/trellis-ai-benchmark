namespace OrderManagement.Application.Customers;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>
/// Customer persistence port.
/// </summary>
public interface ICustomerRepository
{
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    void Add(Customer customer);
}

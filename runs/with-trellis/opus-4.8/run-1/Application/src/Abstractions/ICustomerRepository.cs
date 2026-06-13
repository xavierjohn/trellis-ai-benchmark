namespace OrderManagement.Application.Abstractions;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Persistence operations for the <see cref="Customer"/> aggregate.</summary>
public interface ICustomerRepository
{
    /// <summary>Finds a customer by ID.</summary>
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>Returns true when a customer with the given email already exists.</summary>
    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>Stages a new customer for insertion.</summary>
    void Add(Customer customer);
}

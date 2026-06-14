namespace OrderManagement.Application;

using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>Persistence contract for the <see cref="Customer"/> aggregate.</summary>
public interface ICustomerRepository
{
    /// <summary>Finds a customer by ID, or <see cref="Maybe{T}.None"/> if absent.</summary>
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>Finds a customer by email, or <see cref="Maybe{T}.None"/> if absent.</summary>
    Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>Stages a customer for insertion. The unit-of-work commits on handler success.</summary>
    void Add(Customer customer);
}

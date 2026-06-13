namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>Persistence contract for the <see cref="Customer"/> aggregate.</summary>
public interface ICustomerRepository
{
    /// <summary>Finds a customer by id, or <see cref="Maybe{T}.None"/> when absent.</summary>
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>Returns whether a customer with the given id exists.</summary>
    Task<bool> ExistsAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>Stages a new customer for insertion. The unit of work commits on success.</summary>
    void Add(Customer customer);
}

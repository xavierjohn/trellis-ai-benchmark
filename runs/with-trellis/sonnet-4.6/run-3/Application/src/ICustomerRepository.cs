namespace OrderManagement.Application;

using OrderManagement.Domain;
using Trellis;

/// <summary>Repository interface for Customer persistence.</summary>
public interface ICustomerRepository
{
    Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);
    void Add(Customer customer);
}

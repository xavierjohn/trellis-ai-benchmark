namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

internal sealed class FakeCustomerRepository(FakeRepository<Customer, CustomerId> repository) : ICustomerRepository
{
    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) => repository.FindByIdAsync(id, cancellationToken);

    public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        var customer = repository.GetAll().FirstOrDefault(existing => existing.Email == email);
        return Task.FromResult(customer is null ? Maybe<Customer>.None : Maybe.From(customer));
    }

    public void Add(Customer customer) => repository.Add(customer);
}

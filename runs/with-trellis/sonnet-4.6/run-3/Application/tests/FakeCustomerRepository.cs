namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Testing;

internal class FakeCustomerRepository : ICustomerRepository
{
    private readonly FakeRepository<Customer, CustomerId> _repo;

    public FakeCustomerRepository(FakeRepository<Customer, CustomerId> repo) => _repo = repo;

    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_repo.GetAll().Any(c => c.Email == email));

    public void Add(Customer customer) => _repo.Add(customer);
}

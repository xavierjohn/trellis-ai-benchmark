namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Testing;

internal class FakeOrderRepository : IOrderRepository
{
    private readonly FakeRepository<Order, OrderId> _repo;

    public FakeOrderRepository(FakeRepository<Order, OrderId> repo) => _repo = repo;

    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Order>> FindByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> result = _repo.GetAll().Where(o => o.CustomerId == customerId).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> result = _repo.GetAll().Where(specification.IsSatisfiedBy).ToList();
        return Task.FromResult(result);
    }

    public void Add(Order order) => _repo.Add(order);
}

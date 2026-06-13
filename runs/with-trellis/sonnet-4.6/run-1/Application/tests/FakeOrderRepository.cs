namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Testing;

internal sealed class FakeOrderRepository(FakeRepository<Order, OrderId> repository) : IOrderRepository
{
    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) => repository.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = repository.GetAll().Where(order => order.CustomerId == customerId).ToList();
        return Task.FromResult(orders);
    }

    public Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken) =>
        repository.QueryAsync(specification, cancellationToken);

    public void Add(Order order) => repository.Add(order);
}

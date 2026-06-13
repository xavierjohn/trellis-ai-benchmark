namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Testing;

/// <summary>Adapts <see cref="FakeRepository{Customer, CustomerId}"/> to <see cref="ICustomerRepository"/>.</summary>
internal sealed class FakeCustomerRepository(FakeRepository<Customer, CustomerId> repo) : ICustomerRepository
{
    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        repo.FindByIdAsync(id, cancellationToken);

    public Task<bool> ExistsAsync(CustomerId id, CancellationToken cancellationToken) =>
        Task.FromResult(repo.GetAll().Any(c => c.Id == id));

    public void Add(Customer customer) => repo.Add(customer);
}

/// <summary>Adapts <see cref="FakeRepository{Product, ProductId}"/> to <see cref="IProductRepository"/>.</summary>
internal sealed class FakeProductRepository(FakeRepository<Product, ProductId> repo) : IProductRepository
{
    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) =>
        repo.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken)
    {
        var idSet = ids.ToHashSet();
        IReadOnlyList<Product> matches = repo.GetAll().Where(p => idSet.Contains(p.Id)).ToList();
        return Task.FromResult(matches);
    }

    public void Add(Product product) => repo.Add(product);
}

/// <summary>Adapts <see cref="FakeRepository{Order, OrderId}"/> to <see cref="IOrderRepository"/>.</summary>
internal sealed class FakeOrderRepository(FakeRepository<Order, OrderId> repo) : IOrderRepository
{
    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        repo.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken) =>
        repo.QueryAsync(specification, cancellationToken);

    public void Add(Order order) => repo.Add(order);
}

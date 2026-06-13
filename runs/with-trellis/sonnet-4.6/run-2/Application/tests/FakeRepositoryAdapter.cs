namespace Application.Tests;

using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

/// <summary>
/// Adapts <see cref="FakeRepository{TAggregate, TId}"/> to <see cref="ICustomerRepository"/>.
/// </summary>
internal class FakeCustomerRepositoryAdapter : ICustomerRepository
{
    private readonly FakeRepository<Customer, CustomerId> _repo;

    public FakeCustomerRepositoryAdapter(FakeRepository<Customer, CustomerId> repo) => _repo = repo;

    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken ct) =>
        _repo.FindByIdAsync(id, ct);

    public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken ct)
    {
        var customer = _repo.GetAll().FirstOrDefault(c => c.Email == email);
        return Task.FromResult(Maybe.From(customer));
    }

    public void Add(Customer customer) => _repo.Add(customer);
}

/// <summary>
/// Adapts <see cref="FakeRepository{TAggregate, TId}"/> to <see cref="IProductRepository"/>.
/// </summary>
internal class FakeProductRepositoryAdapter : IProductRepository
{
    private readonly FakeRepository<Product, ProductId> _repo;

    public FakeProductRepositoryAdapter(FakeRepository<Product, ProductId> repo) => _repo = repo;

    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken ct) =>
        _repo.FindByIdAsync(id, ct);

    public Task<Maybe<Product>> FindBySkuAsync(SKU sku, CancellationToken ct)
    {
        var product = _repo.GetAll().FirstOrDefault(p => p.SKU == sku);
        return Task.FromResult(Maybe.From(product));
    }

    public void Add(Product product) => _repo.Add(product);
}

/// <summary>
/// Adapts <see cref="FakeRepository{TAggregate, TId}"/> to <see cref="IOrderRepository"/>.
/// </summary>
internal class FakeOrderRepositoryAdapter : IOrderRepository
{
    private readonly FakeRepository<Order, OrderId> _repo;

    public FakeOrderRepositoryAdapter(FakeRepository<Order, OrderId> repo) => _repo = repo;

    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken ct) =>
        _repo.FindByIdAsync(id, ct);

    public Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> spec, CancellationToken ct) =>
        _repo.QueryAsync(spec, ct);

    public void Add(Order order) => _repo.Add(order);
}


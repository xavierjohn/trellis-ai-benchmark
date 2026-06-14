namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

/// <summary>Adapts the fake repository to <see cref="ICustomerRepository"/>.</summary>
internal sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly FakeRepository<Customer, CustomerId> _repo;
    public FakeCustomerRepository(FakeRepository<Customer, CustomerId> repo) => _repo = repo;

    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        _repo.FindAsync(c => c.Email == email);

    public void Add(Customer customer) => _repo.Add(customer);
}

/// <summary>Adapts the fake repository to <see cref="IProductRepository"/>.</summary>
internal sealed class FakeProductRepository : IProductRepository
{
    private readonly FakeRepository<Product, ProductId> _repo;
    public FakeProductRepository(FakeRepository<Product, ProductId> repo) => _repo = repo;

    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        _repo.FindAsync(p => p.Sku == sku);

    public async Task<IReadOnlyList<Product>> FindManyByIdsAsync(
        IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        (await _repo.WhereAsync(p => ids.Contains(p.Id))).ToList();

    public void Add(Product product) => _repo.Add(product);
}

/// <summary>Adapts the fake repository to <see cref="IOrderRepository"/>.</summary>
internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly FakeRepository<Order, OrderId> _repo;
    public FakeOrderRepository(FakeRepository<Order, OrderId> repo) => _repo = repo;

    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Order>> QueryAsync(Specification<Order> specification, CancellationToken cancellationToken) =>
        _repo.QueryAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        _repo.WhereAsync(o => o.CustomerId == customerId);

    public void Add(Order order) => _repo.Add(order);
}

/// <summary>Shared resource loader for Order authorization in tests.</summary>
internal sealed class FakeOrderResourceLoader : Trellis.Authorization.SharedResourceLoaderById<Order, OrderId>
{
    private readonly FakeRepository<Order, OrderId> _repo;
    public FakeOrderResourceLoader(FakeRepository<Order, OrderId> repo) => _repo = repo;

    public override Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        _repo.GetByIdAsync(id, cancellationToken);
}

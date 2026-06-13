namespace OrderManagement.Application.Tests;

using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Primitives;

/// <summary>In-memory <see cref="ICustomerRepository"/> for handler tests.</summary>
internal sealed class FakeCustomerRepository : ICustomerRepository
{
    public List<Customer> Items { get; } = [];

    public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id) is { } c ? Maybe.From(c) : Maybe<Customer>.None);

    public Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(c => c.Email == email));

    public void Add(Customer customer) => Items.Add(customer);
}

/// <summary>In-memory <see cref="IProductRepository"/> for handler tests.</summary>
internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Items { get; } = [];

    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id) is { } p ? Maybe.From(p) : Maybe<Product>.None);

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Product>>(Items.Where(p => ids.Contains(p.Id)).ToList());

    public Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(p => p.Sku == sku));

    public void Add(Product product) => Items.Add(product);
}

/// <summary>In-memory <see cref="IOrderRepository"/> for handler tests.</summary>
internal sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> Items { get; } = [];

    public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(o => o.Id == id) is { } o ? Maybe.From(o) : Maybe<Order>.None);

    public Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Order>>(Items.Where(o => o.CustomerId == customerId).ToList());

    public Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var spec = new OverdueOrderSpecification(asOf);
        return Task.FromResult<IReadOnlyList<Order>>(Items.Where(spec.IsSatisfiedBy).ToList());
    }

    public void Add(Order order) => Items.Add(order);
}

namespace Application.Tests;

using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;
using Trellis.Testing;

public sealed class OrderManagementApplicationTests
{
    [Fact]
    public async Task Create_customer_duplicate_email_returns_conflict()
    {
        var customers = new FakeCustomerRepository();
        var existing = new Customer(FirstName.Create("Ada"), LastName.Create("Lovelace"), EmailAddress.Create("ada@example.com"), Maybe<PhoneNumber>.None, Address());
        customers.Add(existing);
        var handler = new CreateCustomerCommandHandler(customers);

        var result = await handler.Handle(new CreateCustomerCommand(existing.FirstName, existing.LastName, existing.Email, Maybe<PhoneNumber>.None, existing.ShippingAddress), CancellationToken.None);

        result.Should().BeFailureOfType<Error.Conflict>();
    }

    [Fact]
    public async Task Cancel_by_owner_succeeds_non_owner_fails_admin_succeeds()
    {
        var product = Product();
        product.AddStock(StockAdjustmentQuantity.Create(10)).Should().BeSuccess();
        var order = Order.TryCreate(CustomerId.NewUniqueV7(), [(product, LineItemQuantity.Create(1))], "owner", TimeProvider.System).Unwrap();
        var orders = new FakeOrderRepository(order);
        var products = new FakeProductRepository(product);

        var owner = new CancelOrderCommandHandler(orders, products, new TestActorProvider("owner", Permissions.OrdersCancel), TimeProvider.System);
        (await owner.Handle(new CancelOrderCommand(order.Id), CancellationToken.None)).Should().BeSuccess();

        var otherOrder = Order.TryCreate(CustomerId.NewUniqueV7(), [(product, LineItemQuantity.Create(1))], "owner", TimeProvider.System).Unwrap();
        orders = new FakeOrderRepository(otherOrder);
        var nonOwner = new CancelOrderCommandHandler(orders, products, new TestActorProvider("other", Permissions.OrdersCancel), TimeProvider.System);
        (await nonOwner.Handle(new CancelOrderCommand(otherOrder.Id), CancellationToken.None)).Should().BeFailureOfType<Error.Forbidden>();

        var admin = new CancelOrderCommandHandler(orders, products, new TestActorProvider("admin", Permissions.OrdersCancel, Permissions.OrdersReadAll), TimeProvider.System);
        (await admin.Handle(new CancelOrderCommand(otherOrder.Id), CancellationToken.None)).Should().BeSuccess();
    }

    [Fact]
    public async Task Get_order_not_found_returns_not_found()
    {
        var handler = new GetOrderByIdQueryHandler(new FakeOrderRepository());

        var result = await handler.Handle(new GetOrderByIdQuery(OrderId.NewUniqueV7()), CancellationToken.None);

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public void Commands_declare_required_permissions()
    {
        new SubmitOrderCommand(OrderId.NewUniqueV7()).RequiredPermissions.Should().Contain(Permissions.OrdersSubmit);
        new CreateProductCommand(ProductName.Create("Widget"), Sku.Create("SKU123"), MonetaryAmount.Create(1m))
            .RequiredPermissions.Should().Contain(Permissions.ProductsCreate);
    }

    private static ShippingAddress Address() => new(
        Street.Create("1 Main St"),
        City.Create("Seattle"),
        StateProvince.Create("WA"),
        PostalCode.Create("98101"),
        Country.Create("USA"));

    private static Product Product() =>
        OrderManagement.Domain.Product.TryCreate(ProductName.Create("Widget"), Sku.Create("SKU123"), MonetaryAmount.Create(1m)).Unwrap();

    private sealed class FakeCustomerRepository(params Customer[] customers) : ICustomerRepository
    {
        private readonly List<Customer> _customers = [.. customers];

        public Task<Maybe<Customer>> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
            Task.FromResult(ToMaybe(_customers.FirstOrDefault(c => c.Id == id)));

        public Task<Maybe<Customer>> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
            Task.FromResult(ToMaybe(_customers.FirstOrDefault(c => c.Email == email)));

        public void Add(Customer customer) => _customers.Add(customer);
    }

    private sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        private readonly List<Product> _products = [.. products];

        public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) =>
            Task.FromResult(ToMaybe(_products.FirstOrDefault(p => p.Id == id)));

        public Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Product>>(_products.Where(p => ids.Contains(p.Id)).ToArray());

        public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
            Task.FromResult(ToMaybe(_products.FirstOrDefault(p => p.Sku == sku)));

        public void Add(Product product) => _products.Add(product);
    }

    private sealed class FakeOrderRepository(params Order[] orders) : IOrderRepository
    {
        private readonly List<Order> _orders = [.. orders];

        public Task<Maybe<Order>> FindByIdAsync(OrderId id, CancellationToken cancellationToken) =>
            Task.FromResult(ToMaybe(_orders.FirstOrDefault(o => o.Id == id)));

        public Task<IReadOnlyList<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(_orders.Where(o => o.CustomerId == customerId).ToArray());

        public Task<IReadOnlyList<Order>> ListOverdueAsync(DateTime cutoff, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(_orders.Where(o => o.IsOverdue(cutoff)).ToArray());

        public void Add(Order order) => _orders.Add(order);
    }

    private static Maybe<T> ToMaybe<T>(T? value)
        where T : class =>
        value is null ? Maybe<T>.None : Maybe.From(value);
}

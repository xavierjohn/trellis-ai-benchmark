using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Auth;
using OrderManagement.Api.Domain;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Api.Models;
using OrderManagement.Api.Services;
using FluentValidation;
using Xunit;

namespace OrderManagement.Tests.Application;

/// <summary>
/// Application-level tests using real in-memory SQLite with service layer.
/// Tests authorization permission checks and not-found scenarios.
/// </summary>
public class AuthorizationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly OrderService _orderService;
    private readonly FakeTimeProvider _timeProvider;

    public AuthorizationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _timeProvider = new FakeTimeProvider(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _customerService = new CustomerService(_db, new InlineValidator<CreateCustomerRequest>(v =>
        {
            v.RuleFor(x => x.FirstName).NotEmpty();
            v.RuleFor(x => x.LastName).NotEmpty();
            v.RuleFor(x => x.Email).NotEmpty().EmailAddress();
            v.RuleFor(x => x.ShippingAddress).NotNull();
            v.When(x => x.ShippingAddress != null, () =>
            {
                v.RuleFor(x => x.ShippingAddress.Street).NotEmpty();
                v.RuleFor(x => x.ShippingAddress.City).NotEmpty();
                v.RuleFor(x => x.ShippingAddress.State).NotEmpty();
                v.RuleFor(x => x.ShippingAddress.PostalCode).NotEmpty();
                v.RuleFor(x => x.ShippingAddress.Country).NotEmpty();
            });
        }));

        _productService = new ProductService(_db,
            new InlineValidator<CreateProductRequest>(v =>
            {
                v.RuleFor(x => x.ProductName).NotEmpty();
                v.RuleFor(x => x.Sku).NotEmpty().Length(3, 20).Matches(@"^[A-Z0-9]+$");
                v.RuleFor(x => x.UnitPrice).GreaterThan(0);
            }),
            new InlineValidator<AddStockRequest>(v =>
            {
                v.RuleFor(x => x.Quantity).GreaterThan(0);
            }));

        _orderService = new OrderService(_db, _timeProvider,
            new InlineValidator<CreateOrderRequest>(v =>
            {
                v.RuleFor(x => x.CustomerId).NotEmpty();
                v.RuleFor(x => x.LineItems).NotEmpty();
            }),
            new InlineValidator<AddLineItemRequest>(v =>
            {
                v.RuleFor(x => x.ProductId).NotEmpty();
                v.RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
            }));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Actor SalesRep(string id = "sales-1") => new Actor(id, [
        PermissionConstants.CustomersCreate,
        PermissionConstants.OrdersCreate,
        PermissionConstants.OrdersSubmit,
        PermissionConstants.OrdersCancel,
        PermissionConstants.OrdersRead
    ]);

    private static Actor WarehouseManager(string id = "wh-1") => new Actor(id, [
        PermissionConstants.ProductsCreate,
        PermissionConstants.ProductsManageStock,
        PermissionConstants.OrdersApprove,
        PermissionConstants.OrdersShip,
        PermissionConstants.OrdersDeliver,
        PermissionConstants.OrdersReadAll
    ]);

    private static Actor AdminActor(string id = "admin-1") => new Actor(id, PermissionConstants.All);

    private static Actor NoPermissions(string id = "nobody") => new Actor(id, []);

    private ShippingAddressRequest DefaultAddress() =>
        new("123 Main St", "Springfield", "IL", "62701", "US");

    [Fact]
    public async Task CreateCustomer_WithCorrectPermission_Succeeds()
    {
        var actor = SalesRep();
        var request = new CreateCustomerRequest("John", "Doe", "john@test.com", null, DefaultAddress());

        var customer = await _customerService.CreateCustomer(actor, request);

        Assert.NotNull(customer);
        Assert.Equal("John", customer.FirstName);
    }

    [Fact]
    public async Task CreateCustomer_WithoutPermission_ThrowsForbidden()
    {
        var actor = NoPermissions();
        var request = new CreateCustomerRequest("John", "Doe", "john2@test.com", null, DefaultAddress());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _customerService.CreateCustomer(actor, request));
    }

    [Fact]
    public async Task CreateProduct_WithCorrectPermission_Succeeds()
    {
        var actor = WarehouseManager();
        var request = new CreateProductRequest("Widget", "WGT001", 9.99m);

        var product = await _productService.CreateProduct(actor, request);

        Assert.NotNull(product);
        Assert.Equal("WGT001", product.SKU);
    }

    [Fact]
    public async Task CreateProduct_WithoutPermission_ThrowsForbidden()
    {
        var actor = SalesRep();
        var request = new CreateProductRequest("Widget", "WGT002", 9.99m);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _productService.CreateProduct(actor, request));
    }

    [Fact]
    public async Task GetOrderById_WithCorrectPermission_ReturnsOrder()
    {
        // Setup
        var admin = AdminActor();
        var customer = await _customerService.CreateCustomer(admin,
            new CreateCustomerRequest("Alice", "Smith", "alice@test.com", null, DefaultAddress()));
        var product = await _productService.CreateProduct(admin, new CreateProductRequest("P1", "SKU001", 10m));
        await _productService.AddStock(admin, product.Id, new AddStockRequest(100));
        var order = await _orderService.CreateDraftOrder(admin,
            new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]));

        var actor = SalesRep();
        var found = await _orderService.GetOrderById(actor, order.Id);

        Assert.Equal(order.Id, found.Id);
    }

    [Fact]
    public async Task GetOrderById_WithoutPermission_ThrowsForbidden()
    {
        var admin = AdminActor();
        var customer = await _customerService.CreateCustomer(admin,
            new CreateCustomerRequest("Bob", "Jones", "bob@test.com", null, DefaultAddress()));
        var product = await _productService.CreateProduct(admin, new CreateProductRequest("P2", "SKU002", 10m));
        await _productService.AddStock(admin, product.Id, new AddStockRequest(100));
        var order = await _orderService.CreateDraftOrder(admin,
            new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]));

        var actor = NoPermissions();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _orderService.GetOrderById(actor, order.Id));
    }

    [Fact]
    public async Task GetOrderById_NotFound_ThrowsNotFoundException()
    {
        var actor = SalesRep();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _orderService.GetOrderById(actor, Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelOrder_ByOwner_Succeeds()
    {
        var admin = AdminActor();
        var customer = await _customerService.CreateCustomer(admin,
            new CreateCustomerRequest("Carol", "White", "carol@test.com", null, DefaultAddress()));
        var product = await _productService.CreateProduct(admin, new CreateProductRequest("P3", "SKU003", 10m));
        await _productService.AddStock(admin, product.Id, new AddStockRequest(100));

        var owner = SalesRep("owner-1");
        var order = await _orderService.CreateDraftOrder(owner,
            new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]));

        var cancelled = await _orderService.CancelOrder(owner, order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelOrder_ByNonOwner_ThrowsForbidden()
    {
        var admin = AdminActor();
        var customer = await _customerService.CreateCustomer(admin,
            new CreateCustomerRequest("Dave", "Brown", "dave@test.com", null, DefaultAddress()));
        var product = await _productService.CreateProduct(admin, new CreateProductRequest("P4", "SKU004", 10m));
        await _productService.AddStock(admin, product.Id, new AddStockRequest(100));

        var owner = SalesRep("owner-2");
        var order = await _orderService.CreateDraftOrder(owner,
            new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]));

        var nonOwner = SalesRep("other-sales");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _orderService.CancelOrder(nonOwner, order.Id));
    }

    [Fact]
    public async Task CancelOrder_ByAdmin_Succeeds_RegardlessOfOwnership()
    {
        var admin = AdminActor("admin-canceller");
        var customer = await _customerService.CreateCustomer(admin,
            new CreateCustomerRequest("Eve", "Davis", "eve@test.com", null, DefaultAddress()));
        var product = await _productService.CreateProduct(admin, new CreateProductRequest("P5", "SKU005", 10m));
        await _productService.AddStock(admin, product.Id, new AddStockRequest(100));

        var owner = SalesRep("owner-3");
        var order = await _orderService.CreateDraftOrder(owner,
            new CreateOrderRequest(customer.Id, [new OrderLineItemRequest(product.Id, 1)]));

        // Admin cancels even though they didn't create it
        var cancelled = await _orderService.CancelOrder(admin, order.Id);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }
}

/// <summary>Helper to create inline validators for testing without DI.</summary>
public class InlineValidator<T> : AbstractValidator<T>
{
    public InlineValidator(Action<InlineValidator<T>> config)
    {
        config(this);
    }
}

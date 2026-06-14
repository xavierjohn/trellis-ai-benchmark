using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Api;

public enum OrderStatus { Draft, Submitted, Approved, Shipped, Delivered, Cancelled }

public sealed record ShippingAddress(string Street, string City, string State, string PostalCode, string Country)
{
    public void Validate(Dictionary<string, string[]> errors)
    {
        Required(errors, "shippingAddress.street", Street);
        Required(errors, "shippingAddress.city", City);
        Required(errors, "shippingAddress.state", State);
        Required(errors, "shippingAddress.postalCode", PostalCode);
        Required(errors, "shippingAddress.country", Country);
    }

    private static void Required(Dictionary<string, string[]> errors, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[key] = [$"{key} is required."];
    }
}

public sealed class Customer
{
    private Customer() { }
    private Customer(string firstName, string lastName, string email, string? phoneNumber, ShippingAddress address)
    {
        Id = Guid.NewGuid(); FirstName = firstName.Trim(); LastName = lastName.Trim(); Email = email.Trim().ToLowerInvariant();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Street = address.Street.Trim(); City = address.City.Trim(); State = address.State.Trim(); PostalCode = address.PostalCode.Trim(); Country = address.Country.Trim();
    }
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? PhoneNumber { get; private set; }
    public string Street { get; private set; } = "";
    public string City { get; private set; } = "";
    public string State { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string Country { get; private set; } = "";
    public static Customer Create(string firstName, string lastName, string email, string? phoneNumber, ShippingAddress address)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 100) errors["firstName"] = ["First name is required and must be at most 100 characters."];
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 100) errors["lastName"] = ["Last name is required and must be at most 100 characters."];
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email)) errors["email"] = ["Email must be valid."];
        if (!string.IsNullOrWhiteSpace(phoneNumber) && !Regex.IsMatch(phoneNumber, @"^\+?[0-9 .()\-]{7,25}$")) errors["phoneNumber"] = ["Phone number must be valid."];
        address.Validate(errors);
        if (errors.Count > 0) throw new ValidationProblemException("Customer validation failed.", errors);
        return new Customer(firstName, lastName, email, phoneNumber, address);
    }
}

public sealed class Product
{
    private Product() { }
    private Product(string name, string sku, decimal price) { Id = Guid.NewGuid(); ProductName = name.Trim(); Sku = sku.Trim(); UnitPrice = price; }
    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = "";
    public string Sku { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public static Product Create(string productName, string sku, decimal unitPrice)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 200) errors["productName"] = ["Product name is required and must be at most 200 characters."];
        if (string.IsNullOrWhiteSpace(sku) || !Regex.IsMatch(sku, "^[A-Z0-9]{3,20}$")) errors["sku"] = ["SKU must be 3-20 uppercase alphanumeric characters."];
        if (unitPrice <= 0) errors["unitPrice"] = ["Unit price must be greater than zero."];
        if (errors.Count > 0) throw new ValidationProblemException("Product validation failed.", errors);
        return new Product(productName, sku, unitPrice);
    }
    public void AddStock(int quantity) { if (quantity <= 0) throw new ValidationProblemException("Quantity must be positive.", "quantity"); StockQuantity += quantity; }
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0) throw new ValidationProblemException("Quantity must be positive.", "quantity");
        if (StockQuantity < quantity) throw new ValidationProblemException($"Insufficient stock for product {ProductName}.", "stock");
        StockQuantity -= quantity;
    }
    public void ReleaseStock(int quantity) { if (quantity <= 0) throw new ValidationProblemException("Quantity must be positive.", "quantity"); StockQuantity += quantity; }
}

public sealed class Order
{
    private Order() { }
    private Order(Guid customerId, string actorId, DateTimeOffset createdAt) { Id = Guid.NewGuid(); CustomerId = customerId; CreatedByActorId = actorId; CreatedAt = createdAt; Status = OrderStatus.Draft; }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public List<OrderLineItem> LineItems { get; private set; } = [];
    public decimal OrderTotal => LineItems.Sum(line => line.UnitPrice * line.Quantity);
    public static Order Create(Guid customerId, string actorId, IEnumerable<(Product Product, int Quantity)> items, TimeProvider timeProvider)
    {
        if (customerId == Guid.Empty) throw new ValidationProblemException("Customer is required.", "customerId");
        var list = items.ToList(); ValidateLineItemRequests(list.Select(i => (i.Product.Id, i.Quantity)));
        var order = new Order(customerId, actorId, timeProvider.GetUtcNow());
        foreach (var item in list) order.AddLineItem(item.Product, item.Quantity);
        return order;
    }
    public void AddLineItem(Product product, int quantity)
    {
        EnsureDraft(); ValidateQuantity(quantity);
        if (LineItems.Any(line => line.ProductId == product.Id)) throw new ValidationProblemException("Product already exists in the order. Combine quantities instead.", "productId");
        LineItems.Add(OrderLineItem.Create(product.Id, product.ProductName, quantity, product.UnitPrice));
    }
    public void RemoveLineItem(Guid lineItemId)
    {
        EnsureDraft();
        var line = LineItems.SingleOrDefault(item => item.Id == lineItemId) ?? throw new NotFoundProblemException("Line item was not found.");
        if (LineItems.Count == 1) throw new ValidationProblemException("Cannot remove the last line item from an order.", "lineItemId");
        LineItems.Remove(line);
    }
    public void Submit(IEnumerable<Product> products, TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Draft) throw new ValidationProblemException($"Cannot submit an order from {Status} status.", "status");
        if (LineItems.Count == 0) throw new ValidationProblemException("An order must have at least one line item.", "lineItems");
        var byId = products.ToDictionary(product => product.Id);
        foreach (var line in LineItems) byId[line.ProductId].ReserveStock(line.Quantity);
        Status = OrderStatus.Submitted; SubmittedAt = timeProvider.GetUtcNow();
    }
    public void Approve() { if (Status != OrderStatus.Submitted) throw new ValidationProblemException($"Cannot approve an order from {Status} status.", "status"); Status = OrderStatus.Approved; }
    public void Ship(TimeProvider timeProvider) { if (Status != OrderStatus.Approved) throw new ValidationProblemException($"Cannot ship an order from {Status} status.", "status"); Status = OrderStatus.Shipped; ShippedAt = timeProvider.GetUtcNow(); }
    public void Deliver() { if (Status != OrderStatus.Shipped) throw new ValidationProblemException($"Cannot deliver an order from {Status} status.", "status"); Status = OrderStatus.Delivered; }
    public void Cancel(IEnumerable<Product> products)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled) throw new ValidationProblemException($"Cannot cancel an order from {Status} status.", "status");
        if (Status is OrderStatus.Submitted or OrderStatus.Approved)
        {
            var byId = products.ToDictionary(product => product.Id);
            foreach (var line in LineItems) byId[line.ProductId].ReleaseStock(line.Quantity);
        }
        Status = OrderStatus.Cancelled;
    }
    public static void ValidateLineItemRequests(IEnumerable<(Guid ProductId, int Quantity)> items)
    {
        var list = items.ToList(); var errors = new Dictionary<string, string[]>();
        if (list.Count == 0) errors["lineItems"] = ["At least one line item is required."];
        if (list.Any(i => i.ProductId == Guid.Empty)) errors["productId"] = ["Product ID is required."];
        if (list.Any(i => i.Quantity is < 1 or > 999)) errors["quantity"] = ["Quantity must be between 1 and 999."];
        if (list.Select(i => i.ProductId).Distinct().Count() != list.Count) errors["lineItems"] = ["The same product cannot appear in multiple line items."];
        if (errors.Count > 0) throw new ValidationProblemException("Line item validation failed.", errors);
    }
    private void EnsureDraft() { if (Status != OrderStatus.Draft) throw new ValidationProblemException("Order must be in Draft status.", "status"); }
    private static void ValidateQuantity(int quantity) { if (quantity is < 1 or > 999) throw new ValidationProblemException("Quantity must be between 1 and 999.", "quantity"); }
}

public sealed class OrderLineItem
{
    private OrderLineItem() { }
    private OrderLineItem(Guid productId, string name, int quantity, decimal price) { Id = Guid.NewGuid(); ProductId = productId; ProductName = name; Quantity = quantity; UnitPrice = price; }
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public static OrderLineItem Create(Guid productId, string name, int quantity, decimal price) => new(productId, name, quantity, price);
}

public abstract class ApiProblemException(string message) : Exception(message);
public sealed class ValidationProblemException : ApiProblemException
{
    public ValidationProblemException(string message, Dictionary<string, string[]> errors) : base(message) => Errors = errors;
    public ValidationProblemException(string message, string field) : this(message, new Dictionary<string, string[]> { [field] = [message] }) { }
    public Dictionary<string, string[]> Errors { get; }
}
public sealed class NotFoundProblemException(string message) : ApiProblemException(message);
public sealed class ConflictProblemException(string message) : ApiProblemException(message);
public sealed class ForbiddenProblemException(string message) : ApiProblemException(message);

public static class Permissions
{
    public const string CustomersCreate = "customers:create", ProductsCreate = "products:create", ProductsManageStock = "products:manage-stock", OrdersCreate = "orders:create", OrdersSubmit = "orders:submit", OrdersApprove = "orders:approve", OrdersShip = "orders:ship", OrdersDeliver = "orders:deliver", OrdersCancel = "orders:cancel", OrdersRead = "orders:read", OrdersReadAll = "orders:read-all";
    public static readonly string[] All = [CustomersCreate, ProductsCreate, ProductsManageStock, OrdersCreate, OrdersSubmit, OrdersApprove, OrdersShip, OrdersDeliver, OrdersCancel, OrdersRead, OrdersReadAll];
}
public sealed record Actor(string Id, IReadOnlySet<string> Permissions) { public bool Has(string permission) => Permissions.Contains(permission); }
public interface IActorProvider { Actor GetActor(); }
public sealed class HeaderActorProvider(IHttpContextAccessor accessor) : IActorProvider
{
    private static readonly Actor DefaultAdmin = new("admin", Permissions.All.ToHashSet(StringComparer.OrdinalIgnoreCase));
    public Actor GetActor()
    {
        var context = accessor.HttpContext; if (context is null) return DefaultAdmin;
        if (context.Request.Headers.TryGetValue("X-Test-Actor", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            var payload = JsonSerializer.Deserialize<TestActorPayload>(value!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new ValidationProblemException("X-Test-Actor header is invalid.", "actor");
            return new Actor(payload.Id, payload.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
        }
        var id = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue("oid");
        var permissions = context.User.FindAll(ClaimTypes.Role).Concat(context.User.FindAll("role")).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return string.IsNullOrWhiteSpace(id) || permissions.Count == 0 ? DefaultAdmin : new Actor(id, permissions);
    }
    private sealed record TestActorPayload(string Id, string[] Permissions);
}

public sealed record CreateCustomerRequest(string FirstName, string LastName, string Email, string? PhoneNumber, ShippingAddress ShippingAddress);
public sealed record CreateProductRequest(string ProductName, string Sku, decimal UnitPrice);
public sealed record AddStockRequest(int Quantity);
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineItemRequest> LineItems);
public sealed record OrderLineItemRequest(Guid ProductId, int Quantity);
public sealed record AddLineItemRequest(Guid ProductId, int Quantity);
public sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, ShippingAddress ShippingAddress);
public sealed record ProductResponse(Guid Id, string ProductName, string Sku, decimal UnitPrice, int StockQuantity);
public sealed record OrderResponse(Guid Id, Guid CustomerId, string CreatedByActorId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? SubmittedAt, DateTimeOffset? ShippedAt, decimal OrderTotal, IReadOnlyList<OrderLineItemResponse> LineItems);
public sealed record OrderLineItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
public static class ResponseMapping
{
    public static CustomerResponse ToResponse(this Customer c) => new(c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber, new ShippingAddress(c.Street, c.City, c.State, c.PostalCode, c.Country));
    public static ProductResponse ToResponse(this Product p) => new(p.Id, p.ProductName, p.Sku, p.UnitPrice, p.StockQuantity);
    public static OrderResponse ToResponse(this Order o) => new(o.Id, o.CustomerId, o.CreatedByActorId, o.Status.ToString(), o.CreatedAt, o.SubmittedAt, o.ShippedAt, o.OrderTotal, o.LineItems.OrderBy(l => l.Id).Select(l => new OrderLineItemResponse(l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList());
}

public sealed class OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.Email).IsRequired(); e.Property(x => x.PhoneNumber); });
        modelBuilder.Entity<Product>(e => { e.HasKey(x => x.Id); e.HasIndex(x => x.Sku).IsUnique(); e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)"); });
        modelBuilder.Entity<Order>(e => { e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion<string>(); e.HasMany(x => x.LineItems).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => x.CustomerId); e.HasIndex(x => new { x.Status, x.SubmittedAt }); });
        modelBuilder.Entity<OrderLineItem>(e => { e.HasKey(x => x.Id); e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)"); e.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique(); });
    }
}

public sealed class OrderManagementService(OrderManagementDbContext db, IActorProvider actorProvider, TimeProvider timeProvider)
{
    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest r, CancellationToken ct) { Require(Permissions.CustomersCreate); var c = Customer.Create(r.FirstName, r.LastName, r.Email, r.PhoneNumber, r.ShippingAddress); if (await db.Customers.AnyAsync(x => x.Email == c.Email, ct)) throw new ConflictProblemException("A customer with that email already exists."); db.Customers.Add(c); await SaveAsync(ct); return c.ToResponse(); }
    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest r, CancellationToken ct) { Require(Permissions.ProductsCreate); var p = Product.Create(r.ProductName, r.Sku, r.UnitPrice); if (await db.Products.AnyAsync(x => x.Sku == p.Sku, ct)) throw new ConflictProblemException("A product with that SKU already exists."); db.Products.Add(p); await SaveAsync(ct); return p.ToResponse(); }
    public async Task<ProductResponse> AddStockAsync(Guid id, AddStockRequest r, CancellationToken ct) { Require(Permissions.ProductsManageStock); var p = await db.Products.FindAsync([id], ct) ?? throw new NotFoundProblemException("Product was not found."); p.AddStock(r.Quantity); await SaveAsync(ct); return p.ToResponse(); }
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest r, CancellationToken ct)
    {
        var actor = Require(Permissions.OrdersCreate); if (r.CustomerId == Guid.Empty) throw new ValidationProblemException("Customer is required.", "customerId");
        var items = r.LineItems ?? []; Order.ValidateLineItemRequests(items.Select(i => (i.ProductId, i.Quantity)));
        if (!await db.Customers.AnyAsync(c => c.Id == r.CustomerId, ct)) throw new NotFoundProblemException("Customer was not found.");
        var ids = items.Select(i => i.ProductId).ToList(); var products = await db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        if (products.Count != ids.Count) throw new NotFoundProblemException("One or more products were not found.");
        var byId = products.ToDictionary(p => p.Id); var order = Order.Create(r.CustomerId, actor.Id, items.Select(i => (byId[i.ProductId], i.Quantity)), timeProvider);
        db.Orders.Add(order); await SaveAsync(ct); return order.ToResponse();
    }
    public async Task<OrderResponse> AddLineItemAsync(Guid id, AddLineItemRequest r, CancellationToken ct) { Require(Permissions.OrdersCreate); var o = await FindOrderAsync(id, ct); var p = await db.Products.FindAsync([r.ProductId], ct) ?? throw new NotFoundProblemException("Product was not found."); o.AddLineItem(p, r.Quantity); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> RemoveLineItemAsync(Guid id, Guid lineItemId, CancellationToken ct) { Require(Permissions.OrdersCreate); var o = await FindOrderAsync(id, ct); o.RemoveLineItem(lineItemId); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> SubmitOrderAsync(Guid id, CancellationToken ct) { Require(Permissions.OrdersSubmit); var o = await FindOrderAsync(id, ct); o.Submit(await ProductsForOrderAsync(o, ct), timeProvider); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> ApproveOrderAsync(Guid id, CancellationToken ct) { Require(Permissions.OrdersApprove); var o = await FindOrderAsync(id, ct); o.Approve(); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> ShipOrderAsync(Guid id, CancellationToken ct) { Require(Permissions.OrdersShip); var o = await FindOrderAsync(id, ct); o.Ship(timeProvider); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> DeliverOrderAsync(Guid id, CancellationToken ct) { Require(Permissions.OrdersDeliver); var o = await FindOrderAsync(id, ct); o.Deliver(); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> CancelOrderAsync(Guid id, CancellationToken ct) { var actor = Require(Permissions.OrdersCancel); var o = await FindOrderAsync(id, ct); if (!string.Equals(o.CreatedByActorId, actor.Id, StringComparison.OrdinalIgnoreCase) && !actor.Has(Permissions.OrdersReadAll)) throw new ForbiddenProblemException("Only the order creator or an administrator can cancel the order."); o.Cancel(await ProductsForOrderAsync(o, ct)); await SaveAsync(ct); return o.ToResponse(); }
    public async Task<OrderResponse> GetOrderAsync(Guid id, CancellationToken ct) { Require(Permissions.OrdersRead); return (await FindOrderAsync(id, ct)).ToResponse(); }
    public async Task<IReadOnlyList<OrderResponse>> ListOrdersByCustomerAsync(Guid customerId, CancellationToken ct) { Require(Permissions.OrdersReadAll); if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct)) throw new NotFoundProblemException("Customer was not found."); return (await db.Orders.Include(o => o.LineItems).Where(o => o.CustomerId == customerId).OrderBy(o => o.CreatedAt).ToListAsync(ct)).Select(o => o.ToResponse()).ToList(); }
    public async Task<IReadOnlyList<OrderResponse>> ListOverdueOrdersAsync(CancellationToken ct) { Require(Permissions.OrdersReadAll); var cutoff = timeProvider.GetUtcNow().AddDays(-7); return (await db.Orders.Include(o => o.LineItems).Where(o => o.Status == OrderStatus.Submitted).ToListAsync(ct)).Where(o => o.SubmittedAt < cutoff).OrderBy(o => o.SubmittedAt).Select(o => o.ToResponse()).ToList(); }
    private Actor Require(string permission) { var actor = actorProvider.GetActor(); if (!actor.Has(permission)) throw new ForbiddenProblemException($"Missing required permission '{permission}'."); return actor; }
    private async Task<Order> FindOrderAsync(Guid id, CancellationToken ct) => await db.Orders.Include(o => o.LineItems).SingleOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundProblemException("Order was not found.");
    private async Task<List<Product>> ProductsForOrderAsync(Order order, CancellationToken ct) { var ids = order.LineItems.Select(l => l.ProductId).ToList(); var products = await db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct); if (products.Count != ids.Count) throw new NotFoundProblemException("One or more products were not found."); return products; }
    private async Task SaveAsync(CancellationToken ct) { try { await db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true) { throw new ConflictProblemException("A unique constraint was violated."); } }
}

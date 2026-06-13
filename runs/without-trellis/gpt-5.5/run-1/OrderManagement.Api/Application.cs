using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Api;

public static class Permissions
{
    public const string CustomersCreate = "customers:create";
    public const string ProductsCreate = "products:create";
    public const string ProductsManageStock = "products:manage-stock";
    public const string OrdersCreate = "orders:create";
    public const string OrdersSubmit = "orders:submit";
    public const string OrdersApprove = "orders:approve";
    public const string OrdersShip = "orders:ship";
    public const string OrdersDeliver = "orders:deliver";
    public const string OrdersCancel = "orders:cancel";
    public const string OrdersRead = "orders:read";
    public const string OrdersReadAll = "orders:read-all";

    public static readonly string[] All =
    [
        CustomersCreate, ProductsCreate, ProductsManageStock, OrdersCreate, OrdersSubmit,
        OrdersApprove, OrdersShip, OrdersDeliver, OrdersCancel, OrdersRead, OrdersReadAll
    ];
}

public sealed record Actor(string Id, IReadOnlySet<string> Permissions)
{
    public bool Has(string permission) => Permissions.Contains(permission);
    public static Actor Admin { get; } = new("admin", OrderManagement.Api.Permissions.All.ToHashSet(StringComparer.OrdinalIgnoreCase));
}

public enum ErrorKind { Validation, NotFound, Conflict, Forbidden }

public sealed record AppError(ErrorKind Kind, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);

public readonly record struct AppResult<T>(T? Value, AppError? Error)
{
    public bool IsSuccess => Error is null;
    public static AppResult<T> Success(T value) => new(value, null);
    public static AppResult<T> Failure(AppError error) => new(default, error);
}

public interface ICustomerRepository
{
    Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Product>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Order>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<Order>> ListSubmittedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class OrderManagementService(
    ICustomerRepository customers,
    IProductRepository products,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AppResult<Customer>> CreateCustomerAsync(Actor actor, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.CustomersCreate)) return Forbidden<Customer>();
        try
        {
            var customer = new Customer(request.FirstName, request.LastName, request.Email, request.PhoneNumber,
                new ShippingAddress(request.ShippingAddress.Street, request.ShippingAddress.City, request.ShippingAddress.State, request.ShippingAddress.PostalCode, request.ShippingAddress.Country));
            if (await customers.EmailExistsAsync(customer.Email, cancellationToken))
                return Conflict<Customer>("A customer with this email already exists.");
            await customers.AddAsync(customer, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Customer>.Success(customer);
        }
        catch (DomainException ex) { return Validation<Customer>(ex.Message); }
    }

    public async Task<AppResult<Product>> CreateProductAsync(Actor actor, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.ProductsCreate)) return Forbidden<Product>();
        try
        {
            var product = new Product(request.ProductName, request.Sku, request.UnitPrice);
            if (await products.SkuExistsAsync(product.Sku, cancellationToken))
                return Conflict<Product>("A product with this SKU already exists.");
            await products.AddAsync(product, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Product>.Success(product);
        }
        catch (DomainException ex) { return Validation<Product>(ex.Message); }
    }

    public async Task<AppResult<Product>> AddStockAsync(Actor actor, Guid productId, AddStockRequest request, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.ProductsManageStock)) return Forbidden<Product>();
        var product = await products.GetAsync(productId, cancellationToken);
        if (product is null) return NotFound<Product>("Product not found.");
        try
        {
            product.AddStock(request.Quantity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Product>.Success(product);
        }
        catch (DomainException ex) { return Validation<Product>(ex.Message); }
    }

    public async Task<AppResult<Order>> CreateOrderAsync(Actor actor, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersCreate)) return Forbidden<Order>();
        if (request.CustomerId == Guid.Empty) return Validation<Order>("CustomerId is required.");
        if (request.LineItems.Count == 0) return Validation<Order>("An order must have at least one line item.");
        if (request.LineItems.Select(i => i.ProductId).Distinct().Count() != request.LineItems.Count)
            return Validation<Order>("Duplicate productIds are not allowed.");

        var customer = await customers.GetAsync(request.CustomerId, cancellationToken);
        if (customer is null) return NotFound<Order>("Customer not found.");

        var productIds = request.LineItems.Select(i => i.ProductId).ToArray();
        var productList = await products.GetManyAsync(productIds, cancellationToken);
        if (productList.Count != productIds.Distinct().Count()) return NotFound<Order>("Product not found.");

        try
        {
            var byId = productList.ToDictionary(p => p.Id);
            var items = request.LineItems.Select(i =>
            {
                OrderLineItem.ValidateQuantity(i.Quantity);
                var product = byId[i.ProductId];
                return new OrderLineItem(product.Id, product.ProductName, i.Quantity, product.UnitPrice);
            });
            var order = new Order(request.CustomerId, actor.Id, items, timeProvider.GetUtcNow());
            await orders.AddAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Order>.Success(order);
        }
        catch (DomainException ex) { return Validation<Order>(ex.Message); }
    }

    public async Task<AppResult<Order>> AddLineItemAsync(Actor actor, Guid orderId, AddLineItemRequest request, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersCreate)) return Forbidden<Order>();
        var order = await orders.GetAsync(orderId, cancellationToken);
        if (order is null) return NotFound<Order>("Order not found.");
        var product = await products.GetAsync(request.ProductId, cancellationToken);
        if (product is null) return NotFound<Order>("Product not found.");
        try
        {
            order.AddLineItem(new OrderLineItem(product.Id, product.ProductName, request.Quantity, product.UnitPrice));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Order>.Success(order);
        }
        catch (DomainException ex) { return Validation<Order>(ex.Message); }
    }

    public async Task<AppResult<Order>> RemoveLineItemAsync(Actor actor, Guid orderId, Guid lineItemId, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersCreate)) return Forbidden<Order>();
        var order = await orders.GetAsync(orderId, cancellationToken);
        if (order is null) return NotFound<Order>("Order not found.");
        if (order.LineItems.All(i => i.Id != lineItemId)) return NotFound<Order>("Line item not found.");
        try
        {
            order.RemoveLineItem(lineItemId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Order>.Success(order);
        }
        catch (DomainException ex) { return Validation<Order>(ex.Message); }
    }

    public Task<AppResult<Order>> SubmitAsync(Actor actor, Guid orderId, CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, Permissions.OrdersSubmit, orderId, (order, loadedProducts) => order.Submit(loadedProducts, timeProvider.GetUtcNow()), cancellationToken);

    public Task<AppResult<Order>> ApproveAsync(Actor actor, Guid orderId, CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, Permissions.OrdersApprove, orderId, (order, _) => order.Approve(timeProvider.GetUtcNow()), cancellationToken);

    public Task<AppResult<Order>> ShipAsync(Actor actor, Guid orderId, CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, Permissions.OrdersShip, orderId, (order, _) => order.Ship(timeProvider.GetUtcNow()), cancellationToken);

    public Task<AppResult<Order>> DeliverAsync(Actor actor, Guid orderId, CancellationToken cancellationToken = default) =>
        TransitionAsync(actor, Permissions.OrdersDeliver, orderId, (order, _) => order.Deliver(timeProvider.GetUtcNow()), cancellationToken);

    public async Task<AppResult<Order>> CancelAsync(Actor actor, Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersCancel)) return Forbidden<Order>();
        var order = await orders.GetAsync(orderId, cancellationToken);
        if (order is null) return NotFound<Order>("Order not found.");
        if (order.CreatedByActorId != actor.Id && !actor.Has(Permissions.OrdersReadAll))
            return Forbidden<Order>("Only the creator or an administrator can cancel this order.");
        var loadedProducts = await products.GetManyAsync(order.LineItems.Select(i => i.ProductId), cancellationToken);
        try
        {
            order.Cancel(loadedProducts, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Order>.Success(order);
        }
        catch (DomainException ex) { return Validation<Order>(ex.Message); }
    }

    public async Task<AppResult<Order>> GetOrderAsync(Actor actor, Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersRead)) return Forbidden<Order>();
        var order = await orders.GetAsync(orderId, cancellationToken);
        return order is null ? NotFound<Order>("Order not found.") : AppResult<Order>.Success(order);
    }

    public async Task<AppResult<List<Order>>> ListOrdersByCustomerAsync(Actor actor, Guid customerId, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersReadAll)) return Forbidden<List<Order>>();
        if (await customers.GetAsync(customerId, cancellationToken) is null) return NotFound<List<Order>>("Customer not found.");
        return AppResult<List<Order>>.Success(await orders.ListByCustomerAsync(customerId, cancellationToken));
    }

    public async Task<AppResult<List<Order>>> ListOverdueAsync(Actor actor, CancellationToken cancellationToken = default)
    {
        if (!actor.Has(Permissions.OrdersReadAll)) return Forbidden<List<Order>>();
        return AppResult<List<Order>>.Success(await orders.ListSubmittedBeforeAsync(timeProvider.GetUtcNow().AddDays(-7), cancellationToken));
    }

    private async Task<AppResult<Order>> TransitionAsync(Actor actor, string permission, Guid orderId, Action<Order, List<Product>> transition, CancellationToken cancellationToken)
    {
        if (!actor.Has(permission)) return Forbidden<Order>();
        var order = await orders.GetAsync(orderId, cancellationToken);
        if (order is null) return NotFound<Order>("Order not found.");
        var loadedProducts = await products.GetManyAsync(order.LineItems.Select(i => i.ProductId), cancellationToken);
        try
        {
            transition(order, loadedProducts);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AppResult<Order>.Success(order);
        }
        catch (DomainException ex) { return Validation<Order>(ex.Message); }
    }

    private static AppResult<T> Validation<T>(string message) => AppResult<T>.Failure(new AppError(ErrorKind.Validation, message, new Dictionary<string, string[]> { ["error"] = [message] }));
    private static AppResult<T> NotFound<T>(string message) => AppResult<T>.Failure(new AppError(ErrorKind.NotFound, message));
    private static AppResult<T> Conflict<T>(string message) => AppResult<T>.Failure(new AppError(ErrorKind.Conflict, message));
    private static AppResult<T> Forbidden<T>(string message = "Missing required permission.") => AppResult<T>.Failure(new AppError(ErrorKind.Forbidden, message));
}

public sealed class EfCustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetAsync(Guid id, CancellationToken cancellationToken = default) => db.Customers.FindAsync([id], cancellationToken).AsTask();
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => db.Customers.AnyAsync(c => c.Email == email.ToLower(), cancellationToken);
    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) => db.Customers.AddAsync(customer, cancellationToken).AsTask();
}

public sealed class EfProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default) => db.Products.FindAsync([id], cancellationToken).AsTask();
    public Task<List<Product>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();
        return db.Products.Where(p => idList.Contains(p.Id)).ToListAsync(cancellationToken);
    }
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) => db.Products.AnyAsync(p => p.Sku == sku, cancellationToken);
    public Task AddAsync(Product product, CancellationToken cancellationToken = default) => db.Products.AddAsync(product, cancellationToken).AsTask();
}

public sealed class EfOrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Orders.Include(o => o.LineItems).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<List<Order>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        db.Orders.Include(o => o.LineItems).Where(o => o.CustomerId == customerId).OrderBy(o => o.CreatedAt).ToListAsync(cancellationToken);

    public Task<List<Order>> ListSubmittedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.Orders.Include(o => o.LineItems).Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt < cutoff).OrderBy(o => o.SubmittedAt).ToListAsync(cancellationToken);

    public Task AddAsync(Order order, CancellationToken cancellationToken = default) => db.Orders.AddAsync(order, cancellationToken).AsTask();
}

public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}

namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;
using Trellis.Primitives;

/// <summary>Application use-case service backed by EF Core.</summary>
public sealed class OrderManagementService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    /// <summary>Create the service.</summary>
    public OrderManagementService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    /// <summary>Create a customer.</summary>
    public async Task<Result<Customer>> CreateCustomerAsync(
        FirstName firstName,
        LastName lastName,
        EmailAddress email,
        Maybe<PhoneNumber> phoneNumber,
        ShippingAddress shippingAddress,
        CancellationToken cancellationToken)
    {
        if (await _db.Customers.AnyAsync(c => c.Email == email, cancellationToken))
            return Result.Fail<Customer>(new Error.Conflict(ResourceRef.For<Customer>("email"), "duplicate.email") { Detail = "A customer with this email already exists." });

        var customer = new Customer(firstName, lastName, email, phoneNumber, shippingAddress);
        _db.Customers.Add(customer);
        return await _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => customer);
    }

    /// <summary>Create a product.</summary>
    public async Task<Result<Product>> CreateProductAsync(ProductName productName, Sku sku, UnitPrice unitPrice, CancellationToken cancellationToken)
    {
        if (await _db.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
            return Result.Fail<Product>(new Error.Conflict(ResourceRef.For<Product>("sku"), "duplicate.sku") { Detail = "A product with this SKU already exists." });

        var product = new Product(productName, sku, unitPrice);
        _db.Products.Add(product);
        return await _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => product);
    }

    /// <summary>Add product stock.</summary>
    public async Task<Result<Product>> AddStockAsync(ProductId productId, OrderQuantity quantity, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
            return Result.Fail<Product>(new Error.NotFound(ResourceRef.For<Product>(productId)) { Detail = "Product not found." });

        return await product.AddStock(quantity)
            .BindAsync(_ => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => product));
    }

    /// <summary>Create a draft order.</summary>
    public async Task<Result<Order>> CreateOrderAsync(CustomerId customerId, IReadOnlyList<(ProductId ProductId, OrderQuantity Quantity)> lineItems, string actorId, CancellationToken cancellationToken)
    {
        if (lineItems.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        if (lineItems.Select(i => i.ProductId).Distinct().Count() != lineItems.Count)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));

        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(customerId)) { Detail = "Customer not found." });

        var productIds = lineItems.Select(i => i.ProductId).ToArray();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        if (products.Count != productIds.Length)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>("lineItems.productId")) { Detail = "One or more products were not found." });

        var productById = products.ToDictionary(p => p.Id);
        var items = lineItems.Select(i => (productById[i.ProductId], i.Quantity)).ToList();
        return await Order.TryCreate(customerId, items, actorId, _timeProvider)
            .Tap(order => _db.Orders.Add(order))
            .BindAsync(order => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => order));
    }

    /// <summary>Add a line item to a draft order.</summary>
    public async Task<Result<Order>> AddLineItemAsync(OrderId orderId, ProductId productId, OrderQuantity quantity, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(orderId)) { Detail = "Order not found." });

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(productId)) { Detail = "Product not found." });

        return await order.AddLineItem(product, quantity)
            .BindAsync(_ => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => order));
    }

    /// <summary>Remove a line item from a draft order.</summary>
    public async Task<Result<Order>> RemoveLineItemAsync(OrderId orderId, LineItemId lineItemId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(orderId)) { Detail = "Order not found." });

        return await order.RemoveLineItem(lineItemId)
            .BindAsync(_ => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => order));
    }

    /// <summary>Submit an order.</summary>
    public async Task<Result<Order>> SubmitAsync(OrderId orderId, CancellationToken cancellationToken)
    {
        var loaded = await LoadOrderAndProductsAsync(orderId, cancellationToken);
        if (loaded.Error is not null)
            return Result.Fail<Order>(loaded.Error);

        loaded.TryGetValue(out var state);
        return await state!.Order.Submit(state.Products, _timeProvider)
            .BindAsync(_ => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => state.Order));
    }

    /// <summary>Approve an order.</summary>
    public Task<Result<Order>> ApproveAsync(OrderId orderId, CancellationToken cancellationToken) =>
        MutateOrderAsync(orderId, order => order.Approve(_timeProvider), cancellationToken);

    /// <summary>Ship an order.</summary>
    public Task<Result<Order>> ShipAsync(OrderId orderId, CancellationToken cancellationToken) =>
        MutateOrderAsync(orderId, order => order.Ship(_timeProvider), cancellationToken);

    /// <summary>Deliver an order.</summary>
    public Task<Result<Order>> DeliverAsync(OrderId orderId, CancellationToken cancellationToken) =>
        MutateOrderAsync(orderId, order => order.Deliver(_timeProvider), cancellationToken);

    /// <summary>Cancel an order.</summary>
    public async Task<Result<Order>> CancelAsync(OrderId orderId, CancellationToken cancellationToken)
    {
        var loaded = await LoadOrderAndProductsAsync(orderId, cancellationToken);
        if (loaded.Error is not null)
            return Result.Fail<Order>(loaded.Error);

        loaded.TryGetValue(out var state);
        return await state!.Order.Cancel(state.Products, _timeProvider)
            .BindAsync(_ => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => state.Order));
    }

    /// <summary>Get order by id.</summary>
    public async Task<Result<Order>> GetOrderAsync(OrderId orderId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        return order is null
            ? Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(orderId)) { Detail = "Order not found." })
            : Result.Ok(order);
    }

    /// <summary>List orders for a customer.</summary>
    public async Task<Result<IReadOnlyList<Order>>> ListOrdersByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
            return Result.Fail<IReadOnlyList<Order>>(new Error.NotFound(ResourceRef.For<Customer>(customerId)) { Detail = "Customer not found." });

        var orders = await _db.Orders.Include(o => o.LineItems).Where(o => o.CustomerId == customerId).ToListAsync(cancellationToken);
        return Result.Ok<IReadOnlyList<Order>>(orders);
    }

    /// <summary>List overdue orders.</summary>
    public async Task<Result<IReadOnlyList<Order>>> ListOverdueOrdersAsync(CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
        var orders = await _db.Orders
            .Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt.HasValue && o.SubmittedAt.Value < cutoff)
            .ToListAsync(cancellationToken);

        return Result.Ok<IReadOnlyList<Order>>(orders);
    }

    private async Task<Result<(Order Order, IReadOnlyDictionary<ProductId, Product> Products)>> LoadOrderAndProductsAsync(OrderId orderId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Fail<(Order, IReadOnlyDictionary<ProductId, Product>)>(new Error.NotFound(ResourceRef.For<Order>(orderId)) { Detail = "Order not found." });

        var productIds = order.LineItems.Select(i => i.ProductId).ToArray();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        return Result.Ok((order, (IReadOnlyDictionary<ProductId, Product>)products.ToDictionary(p => p.Id)));
    }

    private async Task<Result<Order>> MutateOrderAsync(OrderId orderId, Func<Order, Result<Order>> mutation, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(orderId)) { Detail = "Order not found." });

        return await mutation(order)
            .BindAsync(_ => _db.SaveChangesResultUnitAsync(cancellationToken).MapAsync(_ => order));
    }

    private Task<Order?> LoadOrderAsync(OrderId orderId, CancellationToken cancellationToken) =>
        _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
}

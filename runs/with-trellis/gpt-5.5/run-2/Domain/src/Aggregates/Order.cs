namespace OrderManagement.Domain;

using Trellis.StateMachine;

/// <summary>Order aggregate.</summary>
public partial class Order : Aggregate<OrderId>
{
    private static class Triggers
    {
        public const string Submit = "Submit";
        public const string Approve = "Approve";
        public const string Ship = "Ship";
        public const string Deliver = "Deliver";
        public const string Cancel = "Cancel";
    }

    private readonly LazyStateMachine<OrderStatus, string> _machine;
    private readonly List<OrderLineItem> _lineItems = [];

    /// <summary>Customer id.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>Actor id that created the order.</summary>
    public string CreatedByActorId { get; private set; } = string.Empty;

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>Submitted timestamp.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>Shipped timestamp.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    /// <summary>Order line items.</summary>
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems.AsReadOnly();

    private Order() : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    /// <summary>Create a draft order.</summary>
    public Order(CustomerId customerId, IEnumerable<(Product Product, OrderQuantity Quantity)> items, string createdByActorId, TimeProvider timeProvider)
        : base(OrderId.NewUniqueV7(timeProvider))
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);

        foreach (var (product, quantity) in items)
            _lineItems.Add(new OrderLineItem(product, quantity));
    }

    /// <summary>Create a validated draft order.</summary>
    public static Result<Order> TryCreate(CustomerId customerId, IReadOnlyCollection<(Product Product, OrderQuantity Quantity)> items, string createdByActorId, TimeProvider timeProvider)
    {
        var validation = ValidateLineItems(items.Select(i => i.Product.Id), items.Count);
        return validation.Map(_ => new Order(customerId, items, createdByActorId, timeProvider));
    }

    /// <summary>Total order amount.</summary>
    public decimal Total => _lineItems.Sum(i => i.LineTotal);

    /// <summary>Add a product to a draft order.</summary>
    public Result<Order> AddLineItem(Product product, OrderQuantity quantity)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Line items can only be added to draft orders."));

        if (_lineItems.Any(i => i.ProductId == product.Id))
            return Result.Fail<Order>(Error.InvalidInput.ForField("productId", "duplicate", "Product already exists in this order."));

        _lineItems.Add(new OrderLineItem(product, quantity));
        return Result.Ok(this);
    }

    /// <summary>Remove a line item from a draft order.</summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Line items can only be removed from draft orders."));

        if (_lineItems.Count <= 1)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.last_line_item", "Cannot remove the last line item."));

        var item = _lineItems.SingleOrDefault(i => i.Id == lineItemId);
        if (item is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<OrderLineItem>(lineItemId)) { Detail = "Line item not found." });

        _lineItems.Remove(item);
        return Result.Ok(this);
    }

    /// <summary>Submit a draft order and reserve stock.</summary>
    public Result<Order> Submit(IReadOnlyDictionary<ProductId, Product> products, TimeProvider timeProvider) =>
        Result.Ensure(_lineItems.Count > 0, Error.InvalidInput.ForRule("order.empty", "Order must have at least one line item."))
            .Bind(_ => EnsureAllProductsHaveStock(products))
            .Bind(_ => _machine.FireResult(Triggers.Submit))
            .Tap(_ =>
            {
                foreach (var item in _lineItems)
                    products[item.ProductId].ApplyReservedStock(item.Quantity);

                var submittedAt = timeProvider.GetUtcNow().UtcDateTime;
                SubmittedAt = submittedAt;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, Total, timeProvider.GetUtcNow()));
            })
            .Map(_ => this);

    /// <summary>Approve a submitted order.</summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Ship an approved order.</summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var shippedAt = timeProvider.GetUtcNow().UtcDateTime;
                ShippedAt = shippedAt;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, timeProvider.GetUtcNow()));
            })
            .Map(_ => this);

    /// <summary>Deliver a shipped order.</summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Cancel a cancellable order and release reserved stock.</summary>
    public Result<Order> Cancel(IReadOnlyDictionary<ProductId, Product> products, TimeProvider timeProvider)
    {
        var fromStatus = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ =>
            {
                if (fromStatus == OrderStatus.Submitted || fromStatus == OrderStatus.Approved)
                {
                    foreach (var item in _lineItems)
                        products[item.ProductId].ApplyReleasedStock(item.Quantity);
                }

                DomainEvents.Add(new OrderCancelledEvent(Id, fromStatus, timeProvider.GetUtcNow()));
            })
            .Map(_ => this);
    }

    /// <summary>Returns true when the order is submitted and older than the cutoff.</summary>
    public bool IsOverdue(DateTime asOf) =>
        Status == OrderStatus.Submitted &&
        SubmittedAt.TryGetValue(out var submittedAt) &&
        submittedAt < asOf.AddDays(-7);

    private Result<Unit> EnsureAllProductsHaveStock(IReadOnlyDictionary<ProductId, Product> products)
    {
        foreach (var item in _lineItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return Result.Fail(new Error.NotFound(ResourceRef.For<Product>(item.ProductId)) { Detail = "Product not found." });

            if (product.StockQuantity.Value < item.Quantity.Value)
                return Result.Fail(Error.InvalidInput.ForRule("product.insufficient_stock", $"Product {product.Sku.Value} has insufficient stock."));
        }

        return Result.Ok();
    }

    private static Result<Unit> ValidateLineItems(IEnumerable<ProductId> productIds, int count)
    {
        if (count == 0)
            return Result.Fail(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        return productIds.Distinct().Count() == count
            ? Result.Ok()
            : Result.Fail(Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));
    }

    private static void ConfigureStateMachine(Stateless.StateMachine<OrderStatus, string> machine)
    {
        machine.Configure(OrderStatus.Draft)
            .Permit(Triggers.Submit, OrderStatus.Submitted)
            .Permit(Triggers.Cancel, OrderStatus.Cancelled);

        machine.Configure(OrderStatus.Submitted)
            .Permit(Triggers.Approve, OrderStatus.Approved)
            .Permit(Triggers.Cancel, OrderStatus.Cancelled);

        machine.Configure(OrderStatus.Approved)
            .Permit(Triggers.Ship, OrderStatus.Shipped)
            .Permit(Triggers.Cancel, OrderStatus.Cancelled);

        machine.Configure(OrderStatus.Shipped)
            .Permit(Triggers.Deliver, OrderStatus.Delivered);

        machine.Configure(OrderStatus.Delivered);
        machine.Configure(OrderStatus.Cancelled);
    }
}

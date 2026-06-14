namespace OrderManagement.Domain;

using Trellis.Primitives;
using Trellis.StateMachine;

/// <summary>
/// Order aggregate with lifecycle state machine.
/// </summary>
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

    /// <summary>Customer that owns the order.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>Actor identity that created the order.</summary>
    public string CreatedByActorId { get; private set; } = null!;

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>Order line items.</summary>
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Submission timestamp, if submitted.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>Shipment timestamp, if shipped.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    private Order() : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    private Order(CustomerId customerId, IEnumerable<OrderLineItem> lineItems, string createdByActorId, TimeProvider timeProvider)
        : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _lineItems.AddRange(lineItems);
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
        _ = timeProvider;
    }

    /// <summary>
    /// Creates a draft order from product snapshots.
    /// </summary>
    public static Result<Order> TryCreate(CustomerId customerId, IReadOnlyCollection<(Product Product, LineItemQuantity Quantity)> items, string createdByActorId, TimeProvider timeProvider) =>
        ValidateLineItems(items)
            .Map(_ => new Order(customerId, items.Select(i => new OrderLineItem(i.Product, i.Quantity)), createdByActorId, timeProvider));

    /// <summary>
    /// Adds a new product line to a draft order.
    /// </summary>
    public Result<Order> AddLineItem(Product product, LineItemQuantity quantity) =>
        EnsureDraft()
            .Ensure(_ => !_lineItems.Any(li => li.ProductId == product.Id),
                Error.InvalidInput.ForField("productId", "duplicate_product", "Product already exists in this order."))
            .Tap(_ => _lineItems.Add(new OrderLineItem(product, quantity)))
            .Map(_ => this);

    /// <summary>
    /// Removes a line item from a draft order.
    /// </summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<OrderLineItem>(lineItemId)) { Detail = $"Line item {lineItemId} not found." });

        return EnsureDraft()
            .Ensure(_ => _lineItems.Count > 1,
                Error.InvalidInput.ForRule("order.cannot_remove_last_line_item", "An order must have at least one line item."))
            .Tap(_ => _lineItems.Remove(lineItem))
            .Map(_ => this);
    }

    /// <summary>
    /// Submits the order and reserves stock for all lines.
    /// </summary>
    public Result<Order> Submit(IReadOnlyDictionary<ProductId, Product> products, TimeProvider timeProvider) =>
        Result.Ok(this)
            .Ensure(order => order._lineItems.Count > 0,
                Error.InvalidInput.ForRule("order.line_items_required", "An order must have at least one line item."))
            .Ensure(_ => _lineItems.All(li => products.TryGetValue(li.ProductId, out var product) && product.HasAvailableStock(li.Quantity)),
                Error.InvalidInput.ForRule("order.insufficient_stock", "One or more products have insufficient stock."))
            .Bind(order => order._machine.FireResult(Triggers.Submit).Map(_ => order))
            .Tap(order =>
            {
                foreach (var line in order._lineItems)
                    products[line.ProductId].ReserveStock(line.Quantity).Discard();

                var submittedAt = timeProvider.GetUtcNow();
                SubmittedAt = submittedAt.UtcDateTime;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, GetTotal(), submittedAt));
            });

    /// <summary>
    /// Approves the order.
    /// </summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>
    /// Ships the order.
    /// </summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var shippedAt = timeProvider.GetUtcNow();
                ShippedAt = shippedAt.UtcDateTime;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, shippedAt));
            })
            .Map(_ => this);

    /// <summary>
    /// Marks the order delivered.
    /// </summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>
    /// Cancels the order and releases stock when needed.
    /// </summary>
    public Result<Order> Cancel(IReadOnlyDictionary<ProductId, Product> products, TimeProvider timeProvider)
    {
        var previousStatus = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ =>
            {
                if (previousStatus == OrderStatus.Submitted || previousStatus == OrderStatus.Approved)
                {
                    foreach (var line in _lineItems)
                        products[line.ProductId].ReleaseStock(line.Quantity).Discard();
                }

                DomainEvents.Add(new OrderCancelledEvent(Id, previousStatus, timeProvider.GetUtcNow()));
            })
            .Map(_ => this);
    }

    /// <summary>
    /// Calculates the order total.
    /// </summary>
    public Money GetTotal() =>
        Money.Sum(_lineItems.Select(line => Money.Create(line.UnitPrice.Value * line.Quantity.Value, "USD")), Money.Create(0m, "USD"))
            .GetValueOrThrow("Order total is calculated from validated non-negative USD line totals.");

    /// <summary>
    /// Returns true when this submitted order is older than the given cutoff.
    /// </summary>
    public bool IsOverdue(DateTime cutoff) =>
        Status == OrderStatus.Submitted &&
        SubmittedAt.HasValue &&
        SubmittedAt.Value < cutoff;

    private static Result<Unit> ValidateLineItems(IReadOnlyCollection<(Product Product, LineItemQuantity Quantity)> items) =>
        Result.Ensure(items.Count > 0, Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."))
            .Combine(Result.Ensure(items.Select(i => i.Product.Id).Distinct().Count() == items.Count,
                Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear in multiple line items.")))
            .Map(_ => Unit.Value);

    private Result<Order> EnsureDraft() =>
        Status == OrderStatus.Draft
            ? Result.Ok(this)
            : Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Order must be in Draft status."));

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

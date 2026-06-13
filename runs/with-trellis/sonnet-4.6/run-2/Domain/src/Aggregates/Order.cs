namespace OrderManagement.Domain;

using Trellis.StateMachine;

/// <summary>
/// An order aggregate with a full lifecycle managed by a state machine.
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
    private List<LineItem> _lineItems = [];

    /// <summary>The customer who placed this order.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>The actor who created this order.</summary>
    public string CreatedByActorId { get; private set; } = null!;

    /// <summary>Current lifecycle status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>When the order was submitted.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>When the order was shipped.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    /// <summary>Line items in this order.</summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Total value of all line items.</summary>
    public decimal OrderTotal => _lineItems.Sum(li => li.UnitPrice.Value * li.Quantity.Value);

    /// <summary>EF Core constructor.</summary>
    private Order() : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(
            () => Status,
            s => Status = s,
            ConfigureStateMachine);
    }

    /// <summary>Creates a new order in Draft status.</summary>
    public Order(CustomerId customerId, string createdByActorId) : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;

        _machine = new LazyStateMachine<OrderStatus, string>(
            () => Status,
            s => Status = s,
            ConfigureStateMachine);
    }

    /// <summary>Adds a line item to this draft order.</summary>
    public Result<Unit> AddLineItem(ProductId productId, ProductName productName, Quantity quantity, UnitPrice unitPrice)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.InvalidInput.ForRule("order.not_draft", "Cannot add line items to a non-draft order."));

        if (_lineItems.Any(li => li.ProductId == productId))
            return Result.Fail(Error.InvalidInput.ForRule("order.duplicate_product", $"Product {productId} is already in this order."));

        _lineItems.Add(new LineItem(productId, productName, quantity, unitPrice));
        return Result.Ok();
    }

    /// <summary>Removes a line item from this draft order.</summary>
    public Result<Unit> RemoveLineItem(LineItemId lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.InvalidInput.ForRule("order.not_draft", "Cannot remove line items from a non-draft order."));

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
            return Result.Fail(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId)) { Detail = $"Line item {lineItemId} not found." });

        if (_lineItems.Count == 1)
            return Result.Fail(Error.InvalidInput.ForRule("order.last_line_item", "Cannot remove the last line item from an order."));

        _lineItems.Remove(item);
        return Result.Ok();
    }

    /// <summary>Submits the order for approval.</summary>
    public Result<Unit> Submit(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Submit)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                SubmittedAt = now.UtcDateTime;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, now.UtcDateTime, now));
            })
            .Map(_ => Unit.Value);

    /// <summary>Approves the submitted order.</summary>
    public Result<Unit> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                DomainEvents.Add(new OrderApprovedEvent(Id, now.UtcDateTime, now));
            })
            .Map(_ => Unit.Value);

    /// <summary>Ships the approved order.</summary>
    public Result<Unit> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                ShippedAt = now.UtcDateTime;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, now.UtcDateTime, now));
            })
            .Map(_ => Unit.Value);

    /// <summary>Marks the order as delivered.</summary>
    public Result<Unit> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                DomainEvents.Add(new OrderDeliveredEvent(Id, now.UtcDateTime, now));
            })
            .Map(_ => Unit.Value);

    /// <summary>
    /// Cancels the order. Returns whether stock should be released and the affected line items.
    /// </summary>
    public Result<(bool releaseStock, IReadOnlyList<(ProductId productId, Quantity quantity)> lineItems)> Cancel(TimeProvider timeProvider)
    {
        var shouldReleaseStock = Status == OrderStatus.Submitted || Status == OrderStatus.Approved;
        var lineItemSnapshot = _lineItems
            .Select(li => (li.ProductId, li.Quantity))
            .ToList();
        var cancelledFromStatus = Status;

        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                DomainEvents.Add(new OrderCancelledEvent(Id, cancelledFromStatus, now.UtcDateTime, now));
            })
            .Map(_ => (shouldReleaseStock, (IReadOnlyList<(ProductId, Quantity)>)lineItemSnapshot));
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

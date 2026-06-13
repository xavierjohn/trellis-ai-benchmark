namespace OrderManagement.Domain;

using Trellis.Primitives;
using Trellis.StateMachine;

/// <summary>
/// An order placed by a customer. Drives a lifecycle state machine and owns its line items.
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
    private readonly List<LineItem> _lineItems = [];

    /// <summary>The customer this order belongs to.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>The identity of the actor who created this order.</summary>
    public string CreatedByActorId { get; private set; } = null!;

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>When the order was submitted, if it has been.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>When the order was shipped, if it has been.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    /// <summary>The line items in this order.</summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems;

    /// <summary>The order total: the sum of (unit price × quantity) over all line items.</summary>
    public MonetaryAmount Total =>
        MonetaryAmount.Create(_lineItems.Sum(li => li.UnitPrice.Value * li.Quantity.Value));

    /// <summary>EF Core constructor.</summary>
    private Order() : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    private Order(OrderId id, CustomerId customerId, string createdByActorId, IEnumerable<LineItem> lineItems)
        : base(id)
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _lineItems.AddRange(lineItems);
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    /// <summary>
    /// Creates a new draft order. Requires at least one line item and no duplicate products.
    /// </summary>
    public static Result<Order> TryCreate(CustomerId customerId, string createdByActorId, IReadOnlyList<LineItem> lineItems)
    {
        if (lineItems.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.empty", "An order must have at least one line item."));

        if (HasDuplicateProducts(lineItems))
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.duplicate-product", "An order cannot contain the same product more than once."));

        return Result.Ok(new Order(OrderId.NewUniqueV7(), customerId, createdByActorId, lineItems));
    }

    /// <summary>
    /// Adds a line item to a draft order. The product must not already be present.
    /// </summary>
    public Result<Order> AddLineItem(LineItem lineItem)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not-draft", "Line items can only be modified while the order is in Draft status."));

        if (_lineItems.Any(li => li.ProductId == lineItem.ProductId))
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.duplicate-product", "This product is already in the order."));

        _lineItems.Add(lineItem);
        return Result.Ok(this);
    }

    /// <summary>
    /// Removes a line item from a draft order. The order must keep at least one line item.
    /// </summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not-draft", "Line items can only be modified while the order is in Draft status."));

        var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId)) { Detail = $"Line item {lineItemId.Value} not found in this order." });

        if (_lineItems.Count == 1)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.last-line-item", "Cannot remove the last line item from an order."));

        _lineItems.Remove(lineItem);
        return Result.Ok(this);
    }

    /// <summary>
    /// Pure predicate: whether the order can be submitted (Draft with at least one line item).
    /// </summary>
    public Result<Unit> CanSubmit() =>
        Status != OrderStatus.Draft
            ? Result.Fail(Error.InvalidInput.ForRule("state.machine.invalid.transition", $"Cannot submit an order in {Status.Value} status."))
            : Result.Ensure(_lineItems.Count > 0, Error.InvalidInput.ForRule("order.empty", "An order must have at least one line item to submit."));

    /// <summary>
    /// Submits the order (Draft → Submitted) and records the submission time.
    /// Stock reservation is orchestrated by the application handler.
    /// </summary>
    public Result<Order> Submit(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Submit)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                SubmittedAt = now.UtcDateTime;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, Total, now));
            })
            .Map(_ => this);

    /// <summary>Approves the order (Submitted → Approved).</summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Ships the order (Approved → Shipped) and records the ship time.</summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                ShippedAt = now.UtcDateTime;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, now));
            })
            .Map(_ => this);

    /// <summary>Marks the order delivered (Shipped → Delivered).</summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>
    /// Indicates whether cancelling this order from its current status releases reserved stock.
    /// True only when the order is Submitted or Approved (stock was reserved on submit).
    /// </summary>
    public bool ReleasesStockOnCancel => Status == OrderStatus.Submitted || Status == OrderStatus.Approved;

    /// <summary>
    /// Cancels the order (Draft/Submitted/Approved → Cancelled). Stock release, when applicable,
    /// is orchestrated by the application handler.
    /// </summary>
    public Result<Order> Cancel(TimeProvider timeProvider)
    {
        var fromStatus = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ => DomainEvents.Add(new OrderCancelledEvent(Id, fromStatus, timeProvider.GetUtcNow())))
            .Map(_ => this);
    }

    private static bool HasDuplicateProducts(IReadOnlyList<LineItem> lineItems) =>
        lineItems.Select(li => li.ProductId).Distinct().Count() != lineItems.Count;

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

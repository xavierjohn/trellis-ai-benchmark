namespace OrderManagement.Domain;

using Stateless;
using Trellis.StateMachine;

/// <summary>
/// An order placed by a customer. Goes through a lifecycle from Draft to Delivered (or Cancelled).
/// Identified by <see cref="OrderId"/>.
/// </summary>
public partial class Order : Aggregate<OrderId>
{
    /// <summary>State machine trigger names.</summary>
    private static class Triggers
    {
        public const string Submit = "Submit";
        public const string Approve = "Approve";
        public const string Ship = "Ship";
        public const string Deliver = "Deliver";
        public const string Cancel = "Cancel";
    }

    private readonly List<LineItem> _lineItems = [];
    private readonly LazyStateMachine<OrderStatus, string> _machine;

    /// <summary>The customer that placed the order.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>Identity of the actor that created the order.</summary>
    public string CreatedByActorId { get; private set; } = null!;

    /// <summary>Current lifecycle status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>UTC timestamp when the order was submitted (absent until submitted).</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>UTC timestamp when the order was shipped (absent until shipped).</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    /// <summary>The line items in this order.</summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems;

    /// <summary>The sum of (unit price × quantity) across all line items.</summary>
    public decimal OrderTotal => _lineItems.Sum(li => li.LineTotal);

    /// <summary>EF Core constructor.</summary>
    private Order() : base(default!)
    {
        _machine = BuildMachine();
    }

    private Order(CustomerId customerId, string createdByActorId)
        : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _machine = BuildMachine();
    }

    private LazyStateMachine<OrderStatus, string> BuildMachine() =>
        new(() => Status, s => Status = s, Configure);

    private static void Configure(StateMachine<OrderStatus, string> machine)
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

    /// <summary>
    /// Creates a draft order with the supplied line items. Requires at least one line item and
    /// no duplicate products. Stock is not reserved at creation time.
    /// </summary>
    public static Result<Order> Create(
        CustomerId customerId,
        string createdByActorId,
        IReadOnlyList<OrderLineInput> lines)
    {
        if (lines.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField(
                "lineItems", "required", "An order must have at least one line item."));

        if (lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
            return Result.Fail<Order>(Error.InvalidInput.ForField(
                "lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));

        var order = new Order(customerId, createdByActorId);
        foreach (var line in lines)
            order._lineItems.Add(new LineItem(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice));

        return Result.Ok(order);
    }

    /// <summary>
    /// Adds a line item to a draft order. The product must not already be present.
    /// </summary>
    public Result<Order> AddLineItem(ProductId productId, ProductName productName, Quantity quantity, UnitPrice unitPrice)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.not_draft", "Line items can only be added to a draft order."));

        if (_lineItems.Any(li => li.ProductId == productId))
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.duplicate_product", "The product is already in the order; combine quantities instead."));

        _lineItems.Add(new LineItem(productId, productName, quantity, unitPrice));
        return Result.Ok(this);
    }

    /// <summary>
    /// Removes a line item from a draft order. The order must keep at least one line item.
    /// </summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.not_draft", "Line items can only be removed from a draft order."));

        if (_lineItems.Count <= 1)
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.last_line_item", "An order must have at least one line item; cannot remove the last one."));

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId.Value.ToString()))
            {
                Detail = "Line item not found.",
            });

        _lineItems.Remove(item);
        return Result.Ok(this);
    }

    /// <summary>
    /// Pure predicate: an order can be submitted only if it is in Draft status (a valid
    /// transition) and has at least one line item.
    /// </summary>
    public Result<Unit> CanSubmit() =>
        Result.Ensure(
                _lineItems.Count > 0,
                Error.InvalidInput.ForField("lineItems", "required", "An order must have at least one line item."))
            .Bind(_ => _machine.Machine.CanFire(Triggers.Submit)
                ? Result.Ok()
                : Result.Fail(Error.InvalidInput.ForRule(
                    "state.machine.invalid.transition",
                    $"Trigger '{Triggers.Submit}' is not permitted from state '{Status}'.")));

    /// <summary>Fires the Draft → Submitted transition and records the submission timestamp.</summary>
    public Result<Order> Submit(TimeProvider timeProvider) =>
        CanSubmit()
            .Bind(_ => _machine.FireResult(Triggers.Submit))
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                SubmittedAt = now;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, now));
            })
            .Map(_ => this);

    /// <summary>Fires the Submitted → Approved transition.</summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow().UtcDateTime)))
            .Map(_ => this);

    /// <summary>Fires the Approved → Shipped transition and records the shipment timestamp.</summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                ShippedAt = now;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, now));
            })
            .Map(_ => this);

    /// <summary>Fires the Shipped → Delivered transition.</summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow().UtcDateTime)))
            .Map(_ => this);

    /// <summary>
    /// Indicates whether cancelling this order should release reserved stock
    /// (true when the order was Submitted or Approved).
    /// </summary>
    public bool ReservesStock => Status == OrderStatus.Submitted || Status == OrderStatus.Approved;

    /// <summary>
    /// Fires the transition to Cancelled. Only permitted from Draft, Submitted, or Approved.
    /// </summary>
    public Result<Order> Cancel(TimeProvider timeProvider)
    {
        var from = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ => DomainEvents.Add(new OrderCancelledEvent(Id, from, timeProvider.GetUtcNow().UtcDateTime)))
            .Map(_ => this);
    }
}

/// <summary>
/// Input describing a single line item used when creating a draft order.
/// </summary>
public sealed record OrderLineInput(ProductId ProductId, ProductName ProductName, Quantity Quantity, UnitPrice UnitPrice);

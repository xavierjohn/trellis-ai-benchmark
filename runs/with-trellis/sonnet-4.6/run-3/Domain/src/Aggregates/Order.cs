namespace OrderManagement.Domain;

using OrderManagement.Domain.Events;
using Trellis.StateMachine;

/// <summary>Order aggregate with state machine lifecycle.</summary>
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

    public CustomerId CustomerId { get; private set; } = null!;
    public string CreatedByActorId { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = null!;
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();
    public partial Maybe<DateTime> SubmittedAt { get; private set; }
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    /// <summary>Order total: sum of (unit price × quantity) for all line items.</summary>
    public decimal OrderTotal => _lineItems.Sum(li => li.Total);

    /// <summary>EF Core constructor.</summary>
    private Order()
        : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(
            () => Status,
            s => Status = s,
            ConfigureStateMachine);
    }

    /// <summary>Creates a new draft order.</summary>
    public Order(CustomerId customerId, string createdByActorId, IEnumerable<LineItem> lineItems)
        : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _lineItems.AddRange(lineItems);

        _machine = new LazyStateMachine<OrderStatus, string>(
            () => Status,
            s => Status = s,
            ConfigureStateMachine);
    }

    /// <summary>Adds a line item to a draft order.</summary>
    public Result<Order> AddLineItem(LineItem lineItem)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Can only add line items to a Draft order."));

        if (_lineItems.Any(li => li.ProductId == lineItem.ProductId))
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.duplicate_product", "Product is already in the order. Combine quantities instead."));

        _lineItems.Add(lineItem);
        return Result.Ok(this);
    }

    /// <summary>Removes a line item from a draft order.</summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Can only remove line items from a Draft order."));

        var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId)) { Detail = $"Line item {lineItemId.Value} not found." });

        if (_lineItems.Count <= 1)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.last_line_item", "Cannot remove the last line item from an order."));

        _lineItems.Remove(lineItem);
        return Result.Ok(this);
    }

    /// <summary>Submits the order (Draft → Submitted). Stock validation and reservation happens in the handler.</summary>
    public Result<OrderStatus> Submit(TimeProvider timeProvider)
    {
        if (_lineItems.Count == 0)
            return Result.Fail<OrderStatus>(Error.InvalidInput.ForRule("order.no_line_items", "Order must have at least one line item."));

        return _machine.FireResult(Triggers.Submit)
            .Tap(_ =>
            {
                var occurredAt = timeProvider.GetUtcNow();
                SubmittedAt = occurredAt.UtcDateTime;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, occurredAt));
            });
    }

    /// <summary>Approves the order (Submitted → Approved).</summary>
    public Result<OrderStatus> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow())));

    /// <summary>Ships the order (Approved → Shipped).</summary>
    public Result<OrderStatus> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var occurredAt = timeProvider.GetUtcNow();
                ShippedAt = occurredAt.UtcDateTime;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, occurredAt));
            });

    /// <summary>Marks the order as delivered (Shipped → Delivered).</summary>
    public Result<OrderStatus> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow())));

    /// <summary>Cancels the order. Handler must release stock if order was Submitted or Approved.</summary>
    public Result<OrderStatus> Cancel(TimeProvider timeProvider)
    {
        var previousStatus = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ => DomainEvents.Add(new OrderCancelledEvent(Id, previousStatus.Value, timeProvider.GetUtcNow())));
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

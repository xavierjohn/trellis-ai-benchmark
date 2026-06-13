namespace OrderManagement.Domain;

using Trellis.Authorization;
using Trellis.StateMachine;

/// <summary>
/// Order aggregate.
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

    /// <summary>Referenced customer.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>Actor who created the order.</summary>
    public ActorId CreatedByActorId { get; private set; } = null!;

    /// <summary>Line items.</summary>
    public IReadOnlyCollection<LineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Current status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>Submitted timestamp.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>Shipped timestamp.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    private Order() : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    /// <summary>Create a draft order with at least one line item.</summary>
    public Order(CustomerId customerId, ActorId createdByActorId, IReadOnlyCollection<(Product Product, LineItemQuantity Quantity)> items, TimeProvider timeProvider)
        : base(OrderId.NewUniqueV7(timeProvider))
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);

        foreach (var (product, quantity) in items)
            _lineItems.Add(new LineItem(product, quantity, timeProvider));
    }

    /// <summary>Create a validated draft order.</summary>
    public static Result<Order> TryCreate(
        Customer customer,
        ActorId createdByActorId,
        IReadOnlyCollection<(Product Product, LineItemQuantity Quantity)> items,
        TimeProvider timeProvider)
    {
        var validation = ValidateLineItems(items);
        return validation.Map(_ => new Order(customer.Id, createdByActorId, items, timeProvider));
    }

    /// <summary>Add a product line to a draft order.</summary>
    public Result<Order> AddLineItem(Product product, LineItemQuantity quantity, TimeProvider timeProvider) =>
        EnsureDraft()
            .Ensure(_ => _lineItems.All(i => i.ProductId != product.Id),
                Error.InvalidInput.ForField("productId", "duplicate", "Product already exists in the order."))
            .Tap(_ => _lineItems.Add(new LineItem(product, quantity, timeProvider)))
            .Map(_ => this);

    /// <summary>Remove a line item from a draft order.</summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        var item = _lineItems.FirstOrDefault(i => i.Id == lineItemId);
        if (item is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId)) { Detail = "Line item not found." });

        return EnsureDraft()
            .Ensure(_ => _lineItems.Count > 1,
                Error.InvalidInput.ForRule("orders.last_line_item", "Cannot remove the last line item from an order."))
            .Tap(_ => _lineItems.Remove(item))
            .Map(_ => this);
    }

    /// <summary>Submit the order.</summary>
    public Result<Order> Submit(TimeProvider timeProvider) =>
        Result.Ensure(_lineItems.Count > 0, Error.InvalidInput.ForRule("orders.empty", "Order must have at least one line item."))
            .Bind(_ => _machine.FireResult(Triggers.Submit))
            .Tap(_ =>
            {
                SubmittedAt = timeProvider.GetUtcNow().UtcDateTime;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, Total, timeProvider.GetUtcNow()));
            })
            .Map(_ => this);

    /// <summary>Approve the order.</summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Ship the order.</summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                ShippedAt = timeProvider.GetUtcNow().UtcDateTime;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, timeProvider.GetUtcNow()));
            })
            .Map(_ => this);

    /// <summary>Deliver the order.</summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Cancel the order.</summary>
    public Result<Order> Cancel(TimeProvider timeProvider)
    {
        var previousStatus = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ => DomainEvents.Add(new OrderCancelledEvent(Id, previousStatus, timeProvider.GetUtcNow())))
            .Map(_ => this);
    }

    /// <summary>Total order value.</summary>
    public decimal Total => _lineItems.Sum(i => i.Total);

    /// <summary>True when submitted more than seven days before the supplied UTC timestamp.</summary>
    public bool IsOverdue(DateTime asOfUtc) =>
        Status == OrderStatus.Submitted &&
        SubmittedAt.TryGetValue(out var submittedAt) &&
        submittedAt < asOfUtc.AddDays(-7);

    private static Result<Unit> ValidateLineItems(IReadOnlyCollection<(Product Product, LineItemQuantity Quantity)> items) =>
        Result.Ensure(items.Count > 0, Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."))
            .Ensure(_ => items.Select(i => i.Product.Id).Distinct().Count() == items.Count,
                Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));

    private Result<Unit> EnsureDraft() =>
        Result.Ensure(Status == OrderStatus.Draft, Error.InvalidInput.ForRule("orders.not_draft", "Order must be in Draft status."));

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

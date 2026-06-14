namespace OrderManagement.Domain;

using Trellis.Authorization;
using Trellis.StateMachine;

/// <summary>Order aggregate.</summary>
public partial class Order : Aggregate<OrderId>
{
    private static class Triggers
    {
        internal const string Submit = "Submit";
        internal const string Approve = "Approve";
        internal const string Ship = "Ship";
        internal const string Deliver = "Deliver";
        internal const string Cancel = "Cancel";
    }

    private readonly List<OrderLineItem> _lineItems = [];
    private readonly LazyStateMachine<OrderStatus, string> _machine;

    private Order() : base(default!)
    {
        CustomerId = null!;
        CreatedByActorId = null!;
        Status = null!;
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    private Order(CustomerId customerId, ActorId createdByActorId, IEnumerable<OrderLineItem> lineItems)
        : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _lineItems.AddRange(lineItems);
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    /// <summary>Customer identifier.</summary>
    public CustomerId CustomerId { get; private set; }

    /// <summary>Actor that created the order.</summary>
    public ActorId CreatedByActorId { get; private set; }

    /// <summary>Current status.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Submitted timestamp.</summary>
    public partial Maybe<DateTimeOffset> SubmittedAt { get; private set; }

    /// <summary>Shipped timestamp.</summary>
    public partial Maybe<DateTimeOffset> ShippedAt { get; private set; }

    /// <summary>Line items.</summary>
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Order total.</summary>
    public decimal Total => _lineItems.Sum(item => item.Total);

    /// <summary>Creates a draft order.</summary>
    public static Result<Order> TryCreate(CustomerId customerId, ActorId createdByActorId, IReadOnlyCollection<(Product Product, OrderQuantity Quantity)> items)
    {
        if (items.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        if (items.Select(i => i.Product.Id).Distinct().Count() != items.Count)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear more than once."));

        var lineItems = items.Select(i => new OrderLineItem(i.Product, i.Quantity)).ToList();
        return Result.Ok(new Order(customerId, createdByActorId, lineItems));
    }

    /// <summary>Adds a line item to a draft order.</summary>
    public Result<Order> AddLineItem(Product product, OrderQuantity quantity) =>
        Result.Ok(this)
            .Ensure(o => o.Status == OrderStatus.Draft, Error.InvalidInput.ForRule("order.not_draft", "Line items can only be added to draft orders."))
            .Ensure(o => o._lineItems.All(item => item.ProductId != product.Id), Error.InvalidInput.ForField("productId", "duplicate_product", "Product already exists in order."))
            .Tap(o => o._lineItems.Add(new OrderLineItem(product, quantity)));

    /// <summary>Removes a line item from a draft order.</summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        var item = _lineItems.SingleOrDefault(line => line.Id == lineItemId);
        if (item is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<OrderLineItem>(lineItemId)) { Detail = $"Line item {lineItemId.Value} not found." });

        return Result.Ok(this)
            .Ensure(o => o.Status == OrderStatus.Draft, Error.InvalidInput.ForRule("order.not_draft", "Line items can only be removed from draft orders."))
            .Ensure(o => o._lineItems.Count > 1, Error.InvalidInput.ForRule("order.last_line_item", "Cannot remove the last line item."))
            .Tap(o => o._lineItems.Remove(item));
    }

    /// <summary>Submits the order after products have been reserved.</summary>
    public Result<Order> Submit(TimeProvider timeProvider) =>
        Result.Ok(this)
            .Ensure(o => o._lineItems.Count > 0, Error.InvalidInput.ForRule("order.empty", "Order must contain at least one line item."))
            .Check(o => o._machine.FireResult(Triggers.Submit))
            .Tap(o =>
            {
                var now = timeProvider.GetUtcNow();
                o.SubmittedAt = now;
                o.DomainEvents.Add(new OrderSubmittedEvent(o.Id, o.CustomerId, o.Total, now));
            });

    /// <summary>Approves the order.</summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Ships the order.</summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(_ =>
            {
                var now = timeProvider.GetUtcNow();
                ShippedAt = now;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, now));
            })
            .Map(_ => this);

    /// <summary>Delivers the order.</summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow())))
            .Map(_ => this);

    /// <summary>Cancels the order.</summary>
    public Result<Order> Cancel(TimeProvider timeProvider)
    {
        var from = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(_ => DomainEvents.Add(new OrderCancelledEvent(Id, from, timeProvider.GetUtcNow())))
            .Map(_ => this);
    }

    /// <summary>True when cancellation must release reserved inventory.</summary>
    public bool ShouldReleaseStockOnCancel() =>
        Status == OrderStatus.Submitted || Status == OrderStatus.Approved;

    /// <summary>Returns true if submitted more than seven days ago.</summary>
    public bool IsOverdue(DateTimeOffset asOf) =>
        Status == OrderStatus.Submitted && SubmittedAt.HasValue && SubmittedAt.Value < asOf.AddDays(-7);

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

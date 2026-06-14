namespace OrderManagement.Domain;

using Stateless;
using Trellis.StateMachine;

/// <summary>A draft line item used when creating an order.</summary>
public sealed record DraftLineItem(ProductId ProductId, ProductName ProductName, Quantity Quantity, UnitPrice UnitPrice);

/// <summary>An order placed by a customer, containing one or more line items.</summary>
public sealed partial class Order : Aggregate<OrderId>
{
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

    /// <summary>The customer who owns this order.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>The identity of the actor who created this order.</summary>
    public string CreatedByActorId { get; private set; } = null!;

    /// <summary>The current order status.</summary>
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    /// <summary>The UTC time the order was submitted, if it has been.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>The UTC time the order was shipped, if it has been.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    /// <summary>The line items in this order.</summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems;

    /// <summary>The order total: the sum of (unit price × quantity) over all line items.</summary>
    public decimal OrderTotal => _lineItems.Sum(li => li.LineTotal);

    private Order() : base(default!) =>
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);

    private Order(CustomerId customerId, string createdByActorId) : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, s => Status = s, ConfigureStateMachine);
    }

    private static void ConfigureStateMachine(StateMachine<OrderStatus, string> machine)
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

    /// <summary>Creates a new draft order with the supplied line items.</summary>
    public static Result<Order> CreateDraft(
        CustomerId customerId, string createdByActorId, IReadOnlyList<DraftLineItem> lines)
    {
        if (lines.Count == 0)
            return Result.Fail<Order>(
                Error.InvalidInput.ForField("lineItems", "required", "An order must have at least one line item."));

        if (lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
            return Result.Fail<Order>(Error.InvalidInput.ForField(
                "lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));

        var order = new Order(customerId, createdByActorId);
        foreach (var line in lines)
            order._lineItems.Add(new LineItem(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice));

        return Result.Ok(order);
    }

    /// <summary>Adds a line item to a draft order. The product must not already be present.</summary>
    public Result<Order> AddLineItem(ProductId productId, ProductName productName, Quantity quantity, UnitPrice unitPrice)
    {
        if (!Equals(Status, OrderStatus.Draft))
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.not_draft", $"Line items can only be added while the order is in Draft status (current: {Status.Value})."));

        if (_lineItems.Any(li => Equals(li.ProductId, productId)))
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.duplicate_product", "The product is already in this order. Combine quantities instead."));

        _lineItems.Add(new LineItem(productId, productName, quantity, unitPrice));
        return Result.Ok(this);
    }

    /// <summary>Removes a line item from a draft order. Cannot remove the last remaining line item.</summary>
    public Result<Order> RemoveLineItem(LineItemId lineItemId)
    {
        var item = _lineItems.FirstOrDefault(li => Equals(li.Id, lineItemId));
        if (item is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId))
            {
                Detail = "Line item not found in this order.",
            });

        if (!Equals(Status, OrderStatus.Draft))
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.not_draft", $"Line items can only be removed while the order is in Draft status (current: {Status.Value})."));

        if (_lineItems.Count <= 1)
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.last_line_item", "Cannot remove the last line item of an order."));

        _lineItems.Remove(item);
        return Result.Ok(this);
    }

    /// <summary>Submits the order: reserves stock for every line item and transitions to Submitted.</summary>
    public Result<Order> Submit(IReadOnlyDictionary<ProductId, Product> products, TimeProvider timeProvider)
    {
        if (!Equals(Status, OrderStatus.Draft))
            return Result.Fail<Order>(Error.InvalidInput.ForRule(
                "order.invalid_transition", $"Cannot submit an order in {Status.Value} status."));

        if (_lineItems.Count == 0)
            return Result.Fail<Order>(
                Error.InvalidInput.ForField("lineItems", "required", "An order must have at least one line item."));

        foreach (var line in _lineItems)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(line.ProductId))
                {
                    Detail = "Referenced product no longer exists.",
                });

            var canReserve = product.CanReserveStock(line.Quantity);
            if (canReserve.TryGetError(out var error))
                return Result.Fail<Order>(error);
        }

        foreach (var line in _lineItems)
            products[line.ProductId].ReserveStock(line.Quantity).Discard();

        return _machine.FireResult(Triggers.Submit)
            .Tap(() =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                SubmittedAt = now;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, now));
            })
            .Map(_ => this);
    }

    /// <summary>Approves a submitted order.</summary>
    public Result<Order> Approve(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Approve)
            .Tap(() => DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow().UtcDateTime)))
            .Map(_ => this);

    /// <summary>Ships an approved order.</summary>
    public Result<Order> Ship(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Ship)
            .Tap(() =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                ShippedAt = now;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, now));
            })
            .Map(_ => this);

    /// <summary>Marks a shipped order as delivered.</summary>
    public Result<Order> Deliver(TimeProvider timeProvider) =>
        _machine.FireResult(Triggers.Deliver)
            .Tap(() => DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow().UtcDateTime)))
            .Map(_ => this);

    /// <summary>
    /// Cancels the order. If it was Submitted or Approved, reserved stock is released back to each product.
    /// </summary>
    public Result<Order> Cancel(IReadOnlyDictionary<ProductId, Product> products, TimeProvider timeProvider)
    {
        var from = Status;
        return _machine.FireResult(Triggers.Cancel)
            .Tap(() =>
            {
                if (Equals(from, OrderStatus.Submitted) || Equals(from, OrderStatus.Approved))
                {
                    foreach (var line in _lineItems)
                        if (products.TryGetValue(line.ProductId, out var product))
                            product.ReleaseStock(line.Quantity).Discard();
                }

                DomainEvents.Add(new OrderCancelledEvent(Id, from, timeProvider.GetUtcNow().UtcDateTime));
            })
            .Map(_ => this);
    }
}

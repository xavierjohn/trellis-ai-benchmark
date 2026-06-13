namespace OrderManagement.Domain;

using Trellis.Authorization;
using Trellis.StateMachine;
using Stateless;

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

    /// <summary>Customer that owns the order.</summary>
    public CustomerId CustomerId { get; private set; } = null!;

    /// <summary>Actor who created the order.</summary>
    public ActorId CreatedByActorId { get; private set; } = null!;

    /// <summary>Current order status.</summary>
    public OrderStatus Status { get; private set; } = null!;

    /// <summary>Order line items.</summary>
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Submission timestamp, when present.</summary>
    public partial Maybe<DateTime> SubmittedAt { get; private set; }

    /// <summary>Shipment timestamp, when present.</summary>
    public partial Maybe<DateTime> ShippedAt { get; private set; }

    private Order() : base(default!)
    {
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, state => Status = state, ConfigureStateMachine);
    }

    /// <summary>
    /// Creates a new draft order.
    /// </summary>
    public Order(CustomerId customerId, ActorId createdByActorId, IEnumerable<LineItem> lineItems)
        : base(OrderId.NewUniqueV7())
    {
        CustomerId = customerId;
        CreatedByActorId = createdByActorId;
        Status = OrderStatus.Draft;
        _lineItems.AddRange(lineItems);
        _machine = new LazyStateMachine<OrderStatus, string>(() => Status, state => Status = state, ConfigureStateMachine);
    }

    /// <summary>Total order amount.</summary>
    public decimal OrderTotal => _lineItems.Sum(lineItem => lineItem.Total);

    /// <summary>
    /// Adds a line item while the order is draft.
    /// </summary>
    public Result<Unit> AddLineItem(ProductId productId, string productName, Quantity quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.InvalidInput.ForRule("order.not_draft", "Can only add line items to a draft order."));

        if (_lineItems.Any(lineItem => lineItem.ProductId == productId))
            return Result.Fail(Error.InvalidInput.ForField("productId", "duplicate_product", "Product already exists in this order."));

        _lineItems.Add(new LineItem(productId, productName, quantity.Value, unitPrice));
        return Result.Ok();
    }

    /// <summary>
    /// Removes a line item while the order is draft.
    /// </summary>
    public Result<Unit> RemoveLineItem(LineItemId lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail(Error.InvalidInput.ForRule("order.not_draft", "Can only remove line items from a draft order."));

        if (_lineItems.Count <= 1)
            return Result.Fail(Error.InvalidInput.ForRule("order.last_line_item", "Cannot remove the last line item from an order."));

        var item = _lineItems.FirstOrDefault(lineItem => lineItem.Id == lineItemId);
        if (item is null)
            return Result.Fail(new Error.NotFound(ResourceRef.For<LineItem>(lineItemId)) { Detail = $"Line item {lineItemId} not found." });

        _lineItems.Remove(item);
        return Result.Ok();
    }

    /// <summary>
    /// Submits the order after validating and reserving stock.
    /// </summary>
    public Result<Unit> Submit(IReadOnlyList<Product> products, TimeProvider timeProvider)
    {
        if (_lineItems.Count == 0)
            return Result.Fail(Error.InvalidInput.ForRule("order.no_line_items", "Order must have at least one line item."));

        foreach (var lineItem in _lineItems)
        {
            var product = products.FirstOrDefault(p => p.Id == lineItem.ProductId);
            if (product is null)
                return Result.Fail(new Error.NotFound(ResourceRef.For<Product>(lineItem.ProductId)) { Detail = $"Product {lineItem.ProductId} not found." });

            if (product.StockQuantity < lineItem.Quantity)
                return Result.Fail(Error.InvalidInput.ForField("quantity", "insufficient_stock", $"Insufficient stock for product {product.Name.Value}. Available: {product.StockQuantity}, requested: {lineItem.Quantity}."));
        }

        foreach (var lineItem in _lineItems)
        {
            var product = products.First(p => p.Id == lineItem.ProductId);
            var reserveResult = product.ReserveStock(lineItem.Quantity);
            if (reserveResult.IsFailure)
                return reserveResult;
        }

        var occurredAt = timeProvider.GetUtcNow();
        return _machine.FireResult(Triggers.Submit)
            .Map(_ => Unit.Default)
            .Tap(_ =>
            {
                SubmittedAt = occurredAt.UtcDateTime;
                DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, OrderTotal, occurredAt));
            });
    }

    /// <summary>
    /// Approves a submitted order.
    /// </summary>
    public Result<Unit> Approve(TimeProvider timeProvider)
    {
        var occurredAt = timeProvider.GetUtcNow();
        return _machine.FireResult(Triggers.Approve)
            .Map(_ => Unit.Default)
            .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, occurredAt)));
    }

    /// <summary>
    /// Marks an approved order as shipped.
    /// </summary>
    public Result<Unit> Ship(TimeProvider timeProvider)
    {
        var occurredAt = timeProvider.GetUtcNow();
        return _machine.FireResult(Triggers.Ship)
            .Map(_ => Unit.Default)
            .Tap(_ =>
            {
                ShippedAt = occurredAt.UtcDateTime;
                DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, occurredAt));
            });
    }

    /// <summary>
    /// Marks a shipped order as delivered.
    /// </summary>
    public Result<Unit> Deliver(TimeProvider timeProvider)
    {
        var occurredAt = timeProvider.GetUtcNow();
        return _machine.FireResult(Triggers.Deliver)
            .Map(_ => Unit.Default)
            .Tap(_ => DomainEvents.Add(new OrderDeliveredEvent(Id, occurredAt)));
    }

    /// <summary>
    /// Cancels the order and releases stock when required.
    /// </summary>
    public Result<Unit> Cancel(IReadOnlyList<Product> products, TimeProvider timeProvider)
    {
        var cancelledFromStatus = Status;
        var occurredAt = timeProvider.GetUtcNow();

        return _machine.FireResult(Triggers.Cancel)
            .Map(_ => Unit.Default)
            .Tap(_ =>
            {
                if (cancelledFromStatus == OrderStatus.Submitted || cancelledFromStatus == OrderStatus.Approved)
                {
                    foreach (var lineItem in _lineItems)
                    {
                        var product = products.FirstOrDefault(p => p.Id == lineItem.ProductId);
                        product?.ReleaseStock(lineItem.Quantity);
                    }
                }

                DomainEvents.Add(new OrderCancelledEvent(Id, cancelledFromStatus.Value, occurredAt));
            });
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
}

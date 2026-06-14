namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Requested line item.</summary>
public sealed record RequestedLineItem(ProductId ProductId, OrderQuantity Quantity);

/// <summary>Create draft order command.</summary>
public sealed record CreateDraftOrderCommand(CustomerId CustomerId, IReadOnlyList<RequestedLineItem> Items)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Add order line item command.</summary>
public sealed record AddLineItemCommand(OrderId OrderId, ProductId ProductId, OrderQuantity Quantity)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Remove order line item command.</summary>
public sealed record RemoveLineItemCommand(OrderId OrderId, LineItemId LineItemId)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Submit order command.</summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];
}

/// <summary>Approve order command.</summary>
public sealed record ApproveOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersApprove];
}

/// <summary>Ship order command.</summary>
public sealed record ShipOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersShip];
}

/// <summary>Deliver order command.</summary>
public sealed record DeliverOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersDeliver];
}

/// <summary>Cancel order command.</summary>
public sealed record CancelOrderCommand(OrderId OrderId)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCancel];

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        actor.IsOwner(resource.CreatedByActorId) || actor.HasPermission(Permissions.OrdersReadAll)
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("orders.cancel.owner", ResourceRef.For<Order>(OrderId)) { Detail = "Only the order creator or an administrator can cancel this order." });
}

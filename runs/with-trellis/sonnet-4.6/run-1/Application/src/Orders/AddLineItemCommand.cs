namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Adds a line item to a draft order.
/// </summary>
public sealed record AddLineItemCommand(OrderId OrderId, ProductId ProductId, Quantity Quantity)
    : ICommand<Result<Order>>, IAuthorize, IAuthorizeResource<Order>, IIdentifyResource<Order, OrderId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, Order resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId), new Error.Forbidden("order.modify.owner", ResourceRef.For<Order>(OrderId))
        {
            Detail = "Only the order creator can modify draft line items.",
        });

    /// <inheritdoc />
    public OrderId GetResourceId() => OrderId;
}

/// <summary>
/// Handles <see cref="AddLineItemCommand"/>.
/// </summary>
public sealed class AddLineItemCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository)
    : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId} not found.",
            });
        }

        var maybeProduct = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (!maybeProduct.TryGetValue(out var product))
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId))
            {
                Detail = $"Product {command.ProductId} not found.",
            });
        }

        return order.AddLineItem(product.Id, product.Name.Value, command.Quantity, product.UnitPrice).Map(_ => order);
    }
}

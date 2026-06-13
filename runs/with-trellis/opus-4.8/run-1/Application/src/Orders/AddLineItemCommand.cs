namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Adds a line item to a draft order.</summary>
public sealed record AddLineItemCommand(OrderId OrderId, ProductId ProductId, Quantity Quantity)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersCreate];
}

/// <summary>Handles <see cref="AddLineItemCommand"/>.</summary>
public sealed class AddLineItemCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository)
    : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var orderMaybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!orderMaybe.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value))
            {
                Detail = "Order not found.",
            });

        var productMaybe = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (!productMaybe.TryGetValue(out var product))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId.Value))
            {
                Detail = "Product not found.",
            });

        return order.AddLineItem(command.ProductId, product.Name, command.Quantity, product.UnitPrice);
    }
}

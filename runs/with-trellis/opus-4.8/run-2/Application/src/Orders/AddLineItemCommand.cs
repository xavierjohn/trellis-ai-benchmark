namespace OrderManagement.Application;

using Mediator;
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
public sealed class AddLineItemCommandHandler(IOrderRepository orders, IProductRepository products)
    : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." });

        var maybeProduct = await products.FindByIdAsync(command.ProductId, cancellationToken);
        if (!maybeProduct.TryGetValue(out var product))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = "Product not found." });

        var lineItem = LineItem.Create(command.ProductId, product.Name, command.Quantity, product.UnitPrice);
        return order.AddLineItem(lineItem);
    }
}

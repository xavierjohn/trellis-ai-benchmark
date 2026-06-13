namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Adds a line item to a draft order.</summary>
public sealed record AddLineItemCommand(
    OrderId OrderId,
    ProductId ProductId,
    int Quantity) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Handler for <see cref="AddLineItemCommand"/>.</summary>
internal sealed class AddLineItemCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository) : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        if (command.Quantity < 1 || command.Quantity > 999)
            return Result.Fail<Order>(Error.InvalidInput.ForField("quantity", "out_of_range", "Quantity must be between 1 and 999."));

        var orderResult = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });
        if (!orderResult.TryGetValue(out var order))
            return Result.Fail<Order>(orderResult.Error!);

        var productResult = await productRepository.FindByIdAsync(command.ProductId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId.Value} not found." });
        if (!productResult.TryGetValue(out var product))
            return Result.Fail<Order>(productResult.Error!);

        return order.AddLineItem(new LineItem(product.Id, product.ProductName, command.Quantity, product.UnitPrice));
    }
}

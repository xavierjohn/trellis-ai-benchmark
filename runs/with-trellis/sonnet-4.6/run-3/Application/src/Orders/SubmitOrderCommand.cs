namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Submits a draft order and reserves stock.</summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];
}

/// <summary>Handler for <see cref="SubmitOrderCommand"/>.</summary>
internal sealed class SubmitOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    TimeProvider timeProvider) : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var orderResult = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId.Value} not found." });
        if (!orderResult.TryGetValue(out var order))
            return Result.Fail<Order>(orderResult.Error!);

        var productIds = order.LineItems.Select(lineItem => lineItem.ProductId).ToList();
        var products = await productRepository.FindByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        foreach (var lineItem in order.LineItems)
        {
            if (!productsById.TryGetValue(lineItem.ProductId, out var product))
            {
                return Result.Fail<Order>(
                    new Error.NotFound(ResourceRef.For<Product>(lineItem.ProductId))
                    {
                        Detail = $"Product {lineItem.ProductId.Value} not found.",
                    });
            }

            if (product.StockQuantity < lineItem.Quantity)
            {
                return Result.Fail<Order>(
                    Error.InvalidInput.ForRule(
                        "stock.insufficient",
                        $"Insufficient stock for product '{product.ProductName.Value}'. Available: {product.StockQuantity}, Requested: {lineItem.Quantity}."));
            }
        }

        var submitResult = order.Submit(timeProvider);
        if (!submitResult.TryGetValue(out _))
            return Result.Fail<Order>(submitResult.Error!);

        foreach (var lineItem in order.LineItems)
        {
            var product = productsById[lineItem.ProductId];
            var reserveResult = product.ReserveStock(lineItem.Quantity);
            if (!reserveResult.TryGetValue(out _))
                return Result.Fail<Order>(reserveResult.Error!);
        }

        return Result.Ok(order);
    }
}

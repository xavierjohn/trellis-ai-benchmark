namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Submits a draft order (Draft → Submitted) and reserves stock for each line item.</summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersSubmit];
}

/// <summary>Handles <see cref="SubmitOrderCommand"/> using the two-pass validate-then-mutate pattern.</summary>
public sealed class SubmitOrderCommandHandler(IOrderRepository orders, IProductRepository products, TimeProvider timeProvider)
    : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeOrder = await orders.FindByIdAsync(command.OrderId, cancellationToken);
        if (!maybeOrder.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = "Order not found." });

        var productIds = order.LineItems.Select(li => li.ProductId).Distinct().ToArray();
        var loaded = await products.GetByIdsAsync(productIds, cancellationToken);
        var byId = loaded.ToDictionary(p => p.Id);

        var missing = productIds.Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length == 1)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(missing[0])) { Detail = "Product not found." });
        if (missing.Length > 1)
            return Result.Fail<Order>(new Error.Aggregate(missing.Select(id =>
                (Error)new Error.NotFound(ResourceRef.For<Product>(id)) { Detail = "Product not found." }).ToArray()));

        var plan = order.LineItems
            .GroupBy(li => li.ProductId)
            .Select(g => (Product: byId[g.Key], Quantity: g.Sum(li => li.Quantity.Value)))
            .ToArray();

        var validation = plan
            .Select(p => p.Product.CanReserveStock(p.Quantity))
            .Append(order.CanSubmit())
            .SequenceAll();

        if (validation.IsFailure)
            return Result.Fail<Order>(validation.Error);

        foreach (var (product, quantity) in plan)
            product.ReserveStock(quantity).Discard();

        return order.Submit(timeProvider);
    }
}

namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Submits a draft order, reserving stock for each line item.</summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersSubmit];
}

/// <summary>Handles <see cref="SubmitOrderCommand"/>.</summary>
public sealed class SubmitOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    TimeProvider timeProvider)
    : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var orderMaybe = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!orderMaybe.TryGetValue(out var order))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId.Value))
            {
                Detail = "Order not found.",
            });

        var ids = order.LineItems.Select(li => li.ProductId).Distinct().ToArray();
        var products = await productRepository.GetByIdsAsync(ids, cancellationToken);
        var byId = products.ToDictionary(p => p.Id);
        var missing = ids.Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(missing[0].Value))
            {
                Detail = "Product not found.",
            });

        // Aggregate duplicate products (defensive — an order forbids duplicate products) so the
        // CanReserve checks operate on the same quantity the matching ReserveStock will deduct.
        var plan = order.LineItems
            .GroupBy(li => li.ProductId)
            .Select(g => (Product: byId[g.Key], Quantity: g.Sum(li => li.Quantity.Value)))
            .ToArray();

        // PASS 1 — validate every reservation plus the submit precondition. No mutations.
        var validation = plan
            .Select(p => p.Product.CanReserve(p.Quantity))
            .Append(order.CanSubmit())
            .SequenceAll();

        if (validation.Error is { } error)
            return Result.Fail<Order>(error);

        // PASS 2 — apply mutations. Each ReserveStock is provably non-failing (validated above).
        foreach (var (product, quantity) in plan)
            product.ReserveStock(quantity).Discard();

        return order.Submit(timeProvider);
    }
}

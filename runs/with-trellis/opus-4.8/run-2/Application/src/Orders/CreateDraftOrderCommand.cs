namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>A requested line in a new order: a product and a quantity.</summary>
public sealed record OrderLineRequest(ProductId ProductId, Quantity Quantity);

/// <summary>Creates a new draft order for a customer. Stock is not reserved until submission.</summary>
public sealed record CreateDraftOrderCommand(CustomerId CustomerId, IReadOnlyList<OrderLineRequest> Lines)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersCreate];
}

/// <summary>Handles <see cref="CreateDraftOrderCommand"/>.</summary>
public sealed class CreateDraftOrderCommandHandler(
    ICustomerRepository customers,
    IProductRepository products,
    IActorProvider actorProvider,
    IOrderRepository orders) : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.empty", "An order must have at least one line item."));

        if (command.Lines.Select(l => l.ProductId).Distinct().Count() != command.Lines.Count)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.duplicate-product", "An order cannot contain the same product more than once."));

        var customerExists = await customers.ExistsAsync(command.CustomerId, cancellationToken);
        if (!customerExists)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId)) { Detail = "Customer not found." });

        var productIds = command.Lines.Select(l => l.ProductId).ToArray();
        var loaded = await products.GetByIdsAsync(productIds, cancellationToken);
        var byId = loaded.ToDictionary(p => p.Id);

        var missing = productIds.Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length == 1)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(missing[0])) { Detail = "Product not found." });
        if (missing.Length > 1)
            return Result.Fail<Order>(new Error.Aggregate(missing.Select(id =>
                (Error)new Error.NotFound(ResourceRef.For<Product>(id)) { Detail = "Product not found." }).ToArray()));

        var actor = await actorProvider.GetCurrentActorAsync(cancellationToken);
        if (!actor.TryGetValue(out var currentActor))
            return Result.Fail<Order>(new Error.AuthenticationRequired());

        var lineItems = command.Lines
            .Select(l =>
            {
                var product = byId[l.ProductId];
                return LineItem.Create(l.ProductId, product.Name, l.Quantity, product.UnitPrice);
            })
            .ToArray();

        return Order.TryCreate(command.CustomerId, currentActor.Id.Value, lineItems)
            .Tap(orders.Add);
    }
}

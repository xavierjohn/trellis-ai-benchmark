namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Creates a draft order for a customer with one or more line items.</summary>
public sealed record CreateDraftOrderCommand(CustomerId CustomerId, IReadOnlyList<OrderLineRequest> Lines)
    : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.OrdersCreate];
}

/// <summary>Handles <see cref="CreateDraftOrderCommand"/>.</summary>
public sealed class CreateDraftOrderCommandHandler(
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IActorProvider actorProvider)
    : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField(
                "lineItems", "required", "An order must have at least one line item."));

        var ids = command.Lines.Select(l => l.ProductId).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            return Result.Fail<Order>(Error.InvalidInput.ForField(
                "lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));

        var actorMaybe = await actorProvider.GetCurrentActorAsync(cancellationToken);
        if (!actorMaybe.TryGetValue(out var actor))
            return Result.Fail<Order>(new Error.AuthenticationRequired { Detail = "Authentication required." });

        var customerMaybe = await customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (!customerMaybe.TryGetValue(out _))
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId.Value))
            {
                Detail = "Customer not found.",
            });

        var products = await productRepository.GetByIdsAsync(ids, cancellationToken);
        var byId = products.ToDictionary(p => p.Id);
        var missing = ids.Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(missing[0].Value))
            {
                Detail = "Product not found.",
            });

        var lines = command.Lines
            .Select(l => new OrderLineInput(l.ProductId, byId[l.ProductId].Name, l.Quantity, byId[l.ProductId].UnitPrice))
            .ToList();

        return Order.Create(command.CustomerId, actor.Id.Value, lines).Tap(orderRepository.Add);
    }
}

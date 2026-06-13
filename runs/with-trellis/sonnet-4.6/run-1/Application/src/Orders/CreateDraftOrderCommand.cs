namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Input line item for order creation.
/// </summary>
public sealed record CreateDraftOrderInput(ProductId ProductId, Quantity Quantity);

/// <summary>
/// Creates a draft order.
/// </summary>
public sealed class CreateDraftOrderCommand : ICommand<Result<Order>>, IAuthorize
{
    /// <summary>Customer identifier.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>Initial line items.</summary>
    public IReadOnlyList<CreateDraftOrderInput> LineItems { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];

    private CreateDraftOrderCommand(CustomerId customerId, IReadOnlyList<CreateDraftOrderInput> lineItems)
    {
        CustomerId = customerId;
        LineItems = lineItems;
    }

    /// <summary>
    /// Builds a validated command.
    /// </summary>
    public static Result<CreateDraftOrderCommand> TryCreate(CustomerId customerId, IReadOnlyList<CreateDraftOrderInput> lineItems)
    {
        if (lineItems.Count == 0)
            return Result.Fail<CreateDraftOrderCommand>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        if (lineItems.GroupBy(lineItem => lineItem.ProductId).Any(group => group.Count() > 1))
            return Result.Fail<CreateDraftOrderCommand>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "Duplicate products in the same order are not allowed."));

        return Result.Ok(new CreateDraftOrderCommand(customerId, lineItems));
    }
}

/// <summary>
/// Handles <see cref="CreateDraftOrderCommand"/>.
/// </summary>
public sealed class CreateDraftOrderCommandHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IActorProvider actorProvider) : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        var actor = (await actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("Actor must be present; authorization behavior guarantees this.");

        var maybeCustomer = await customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (!maybeCustomer.HasValue)
        {
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId))
            {
                Detail = $"Customer {command.CustomerId} not found.",
            });
        }

        var productIds = command.LineItems.Select(lineItem => lineItem.ProductId).ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(product => product.Id);

        foreach (var lineItem in command.LineItems)
        {
            if (!productLookup.ContainsKey(lineItem.ProductId))
            {
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(lineItem.ProductId))
                {
                    Detail = $"Product {lineItem.ProductId} not found.",
                });
            }
        }

        var orderLineItems = command.LineItems
            .Select(lineItem =>
            {
                var product = productLookup[lineItem.ProductId];
                return new LineItem(product.Id, product.Name.Value, lineItem.Quantity.Value, product.UnitPrice);
            })
            .ToList();

        var order = new Order(command.CustomerId, actor.Id, orderLineItems);
        orderRepository.Add(order);
        return Result.Ok(order);
    }
}

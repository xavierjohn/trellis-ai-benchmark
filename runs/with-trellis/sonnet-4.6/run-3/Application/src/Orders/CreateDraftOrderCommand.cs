namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Input line item for order creation.</summary>
public sealed record OrderLineItemInput(ProductId ProductId, int Quantity);

/// <summary>Creates a new draft order.</summary>
public sealed class CreateDraftOrderCommand : ICommand<Result<Order>>, IAuthorize
{
    public CustomerId CustomerId { get; }

    public IReadOnlyList<OrderLineItemInput> LineItems { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];

    private CreateDraftOrderCommand(CustomerId customerId, IReadOnlyList<OrderLineItemInput> lineItems)
    {
        CustomerId = customerId;
        LineItems = lineItems;
    }

    public static Result<CreateDraftOrderCommand> TryCreate(CustomerId customerId, IReadOnlyList<OrderLineItemInput> lineItems)
    {
        if (lineItems.Count == 0)
            return Result.Fail<CreateDraftOrderCommand>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        if (lineItems.GroupBy(lineItem => lineItem.ProductId).Any(group => group.Count() > 1))
            return Result.Fail<CreateDraftOrderCommand>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "Duplicate product IDs are not allowed."));

        if (lineItems.Any(lineItem => lineItem.Quantity < 1 || lineItem.Quantity > 999))
            return Result.Fail<CreateDraftOrderCommand>(Error.InvalidInput.ForField("lineItems.quantity", "out_of_range", "Quantity must be between 1 and 999."));

        return Result.Ok(new CreateDraftOrderCommand(customerId, lineItems));
    }
}

/// <summary>Handler for <see cref="CreateDraftOrderCommand"/>.</summary>
internal sealed class CreateDraftOrderCommandHandler(
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IActorProvider actorProvider) : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        var maybeActor = await actorProvider.GetCurrentActorAsync(cancellationToken);
        if (!maybeActor.TryGetValue(out var actor))
            return Result.Fail<Order>(new Error.AuthenticationRequired { Detail = "Authentication required." });

        var customerResult = await customerRepository.FindByIdAsync(command.CustomerId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId)) { Detail = $"Customer {command.CustomerId.Value} not found." });
        if (!customerResult.TryGetValue(out _))
            return Result.Fail<Order>(customerResult.Error!);

        var productIds = command.LineItems.Select(lineItem => lineItem.ProductId).ToList();
        var products = await productRepository.FindByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        foreach (var productId in productIds)
        {
            if (!productsById.ContainsKey(productId))
            {
                return Result.Fail<Order>(
                    new Error.NotFound(ResourceRef.For<Product>(productId))
                    {
                        Detail = $"Product {productId.Value} not found.",
                    });
            }
        }

        var lineItems = command.LineItems
            .Select(input =>
            {
                var product = productsById[input.ProductId];
                return new LineItem(product.Id, product.ProductName, input.Quantity, product.UnitPrice);
            })
            .ToList();

        var order = new Order(command.CustomerId, actor.Id.Value, lineItems);
        orderRepository.Add(order);
        return Result.Ok(order);
    }
}

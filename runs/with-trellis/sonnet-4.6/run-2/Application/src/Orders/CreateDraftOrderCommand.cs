namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Creates a new draft order.</summary>
public sealed record CreateDraftOrderCommand(
    CustomerId CustomerId,
    IReadOnlyList<(ProductId ProductId, Quantity Quantity)> LineItems) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Handler for CreateDraftOrderCommand.</summary>
public sealed class CreateDraftOrderCommandHandler : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IActorProvider _actorProvider;

    public CreateDraftOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IActorProvider actorProvider)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _actorProvider = actorProvider;
    }

    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.LineItems.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        var duplicateProductIds = command.LineItems
            .GroupBy(li => li.ProductId.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProductIds.Count > 0)
            return Result.Fail<Order>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "Duplicate product IDs are not allowed in the same order."));

        var customerMaybe = await _customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (customerMaybe.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId)) { Detail = $"Customer {command.CustomerId} not found." });

        var actor = (await _actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("Actor must be present; IAuthorize pipeline guarantees this.");

        var order = new Order(command.CustomerId, actor.Id);

        foreach (var (productId, quantity) in command.LineItems)
        {
            var productMaybe = await _productRepository.FindByIdAsync(productId, cancellationToken);
            if (productMaybe.HasNoValue)
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(productId)) { Detail = $"Product {productId} not found." });

            var product = productMaybe.GetValueOrThrow();
            var addResult = order.AddLineItem(product.Id, product.ProductName, quantity, product.UnitPrice);
            if (addResult.IsFailure)
                return Result.Fail<Order>(addResult.Error);
        }

        _orderRepository.Add(order);
        return Result.Ok(order);
    }
}

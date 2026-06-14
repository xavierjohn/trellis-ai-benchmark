namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Adds a line item to a draft order.</summary>
public sealed record AddLineItemCommand(
    OrderId OrderId,
    ProductId ProductId,
    Quantity Quantity) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Handler for <see cref="AddLineItemCommand"/>.</summary>
public sealed class AddLineItemCommandHandler : ICommandHandler<AddLineItemCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    /// <summary>Creates the handler.</summary>
    public AddLineItemCommandHandler(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(AddLineItemCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!order.HasValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            });

        var product = await _productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (!product.HasValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId))
            {
                Detail = $"Product {command.ProductId.Value} not found.",
            });

        var orderValue = order.GetValueOrThrow();
        var productValue = product.GetValueOrThrow();
        return orderValue.AddLineItem(productValue.Id, productValue.Name, command.Quantity, productValue.UnitPrice);
    }
}

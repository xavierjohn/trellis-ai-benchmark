namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Submits an order for approval, reserving stock for each line item.</summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];
}

/// <summary>Handler for SubmitOrderCommand.</summary>
public sealed class SubmitOrderCommandHandler : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var orderMaybe = await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (orderMaybe.HasNoValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId)) { Detail = $"Order {command.OrderId} not found." });

        var order = orderMaybe.GetValueOrThrow();

        foreach (var lineItem in order.LineItems)
        {
            var productMaybe = await _productRepository.FindByIdAsync(lineItem.ProductId, cancellationToken);
            if (productMaybe.HasNoValue)
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(lineItem.ProductId)) { Detail = $"Product {lineItem.ProductId} not found." });

            var reserveResult = productMaybe.GetValueOrThrow().ReserveStock(lineItem.Quantity);
            if (reserveResult.IsFailure)
                return Result.Fail<Order>(reserveResult.Error);
        }

        return order.Submit(_timeProvider).Map(_ => order);
    }
}

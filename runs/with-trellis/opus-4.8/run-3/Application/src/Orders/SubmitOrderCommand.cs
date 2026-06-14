namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Submits a draft order, reserving stock for each line item.</summary>
public sealed record SubmitOrderCommand(OrderId OrderId) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersSubmit];
}

/// <summary>Handler for <see cref="SubmitOrderCommand"/>.</summary>
public sealed class SubmitOrderCommandHandler : ICommandHandler<SubmitOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the handler.</summary>
    public SubmitOrderCommandHandler(
        IOrderRepository orderRepository, IProductRepository productRepository, TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
        if (!order.HasValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Order>(command.OrderId))
            {
                Detail = $"Order {command.OrderId.Value} not found.",
            });

        var orderValue = order.GetValueOrThrow();
        var productIds = orderValue.LineItems.Select(li => li.ProductId).Distinct().ToArray();
        var products = (await _productRepository.FindManyByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        return orderValue.Submit(products, _timeProvider);
    }
}

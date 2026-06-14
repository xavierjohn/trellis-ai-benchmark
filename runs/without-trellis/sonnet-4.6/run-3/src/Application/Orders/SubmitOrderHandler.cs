using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class SubmitOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitOrderHandler(IOrderRepository orderRepository, IProductRepository productRepository, TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Order>> HandleAsync(Guid orderId, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:submit"))
        {
            return Result<Order>.Forbidden("You do not have permission to submit orders.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {orderId} not found.");
        }

        var productIds = order.LineItems.Select(li => li.ProductId);
        var products = await _productRepository.GetByIdsAsync(productIds, ct);

        var result = order.Submit(products, _timeProvider);
        if (!result.IsSuccess)
        {
            return Result<Order>.Failure(result.Error!, result.ErrorCode!);
        }

        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(order);
    }
}

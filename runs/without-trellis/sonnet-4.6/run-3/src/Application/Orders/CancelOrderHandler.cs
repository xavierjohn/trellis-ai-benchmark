using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class CancelOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public CancelOrderHandler(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<Order>> HandleAsync(Guid orderId, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:cancel"))
        {
            return Result<Order>.Forbidden("You do not have permission to cancel orders.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {orderId} not found.");
        }

        var canCancel = actor.HasPermission("orders:read-all") || order.CreatedByActorId == actor.Id;
        if (!canCancel)
        {
            return Result<Order>.Forbidden("You can only cancel orders you created.");
        }

        var productIds = order.LineItems.Select(li => li.ProductId);
        var products = await _productRepository.GetByIdsAsync(productIds, ct);

        var result = order.Cancel(products);
        if (!result.IsSuccess)
        {
            return Result<Order>.Failure(result.Error!, result.ErrorCode!);
        }

        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(order);
    }
}

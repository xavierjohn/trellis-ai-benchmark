using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class ApproveOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public ApproveOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<Order>> HandleAsync(Guid orderId, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:approve"))
        {
            return Result<Order>.Forbidden("You do not have permission to approve orders.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {orderId} not found.");
        }

        var result = order.Approve();
        if (!result.IsSuccess)
        {
            return Result<Order>.Failure(result.Error!, result.ErrorCode!);
        }

        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(order);
    }
}

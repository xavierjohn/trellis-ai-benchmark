using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class ShipOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly TimeProvider _timeProvider;

    public ShipOrderHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Order>> HandleAsync(Guid orderId, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:ship"))
        {
            return Result<Order>.Forbidden("You do not have permission to ship orders.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {orderId} not found.");
        }

        var result = order.Ship(_timeProvider);
        if (!result.IsSuccess)
        {
            return Result<Order>.Failure(result.Error!, result.ErrorCode!);
        }

        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(order);
    }
}

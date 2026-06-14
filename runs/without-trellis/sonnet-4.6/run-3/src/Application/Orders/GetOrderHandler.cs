using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class GetOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<Order>> HandleAsync(Guid orderId, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:read") && !actor.HasPermission("orders:read-all"))
        {
            return Result<Order>.Forbidden("You do not have permission to read orders.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {orderId} not found.");
        }

        return Result<Order>.Success(order);
    }
}

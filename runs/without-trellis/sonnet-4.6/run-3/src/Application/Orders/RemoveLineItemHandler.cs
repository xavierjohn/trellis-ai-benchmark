using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class RemoveLineItemHandler
{
    private readonly IOrderRepository _orderRepository;

    public RemoveLineItemHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<Order>> HandleAsync(RemoveLineItemCommand command, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:create"))
        {
            return Result<Order>.Forbidden("You do not have permission to modify orders.");
        }

        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {command.OrderId} not found.");
        }

        var result = order.RemoveLineItem(command.LineItemId);
        if (!result.IsSuccess)
        {
            return Result<Order>.Failure(result.Error!, result.ErrorCode!);
        }

        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(order);
    }
}

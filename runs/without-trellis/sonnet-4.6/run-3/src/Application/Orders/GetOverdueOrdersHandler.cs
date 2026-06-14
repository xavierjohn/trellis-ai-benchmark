using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class GetOverdueOrdersHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly TimeProvider _timeProvider;

    public GetOverdueOrdersHandler(IOrderRepository orderRepository, TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<List<Order>>> HandleAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:read-all"))
        {
            return Result<List<Order>>.Forbidden("You do not have permission to read all orders.");
        }

        var orders = await _orderRepository.GetOverdueAsync(_timeProvider, ct);
        return Result<List<Order>>.Success(orders);
    }
}

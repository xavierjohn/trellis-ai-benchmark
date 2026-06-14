using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class GetCustomerOrdersHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerOrdersHandler(IOrderRepository orderRepository, ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
    }

    public async Task<Result<List<Order>>> HandleAsync(Guid customerId, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:read-all"))
        {
            return Result<List<Order>>.Forbidden("You do not have permission to read all orders.");
        }

        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer == null)
        {
            return Result<List<Order>>.NotFound($"Customer {customerId} not found.");
        }

        var orders = await _orderRepository.GetByCustomerIdAsync(customerId, ct);
        return Result<List<Order>>.Success(orders);
    }
}

using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class CreateOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly TimeProvider _timeProvider;

    public CreateOrderHandler(IOrderRepository orderRepository, ICustomerRepository customerRepository, TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Order>> HandleAsync(CreateOrderCommand command, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:create"))
        {
            return Result<Order>.Forbidden("You do not have permission to create orders.");
        }

        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, ct);
        if (customer == null)
        {
            return Result<Order>.NotFound($"Customer {command.CustomerId} not found.");
        }

        var orderResult = Order.Create(command.CustomerId, actor.Id, _timeProvider);
        if (!orderResult.IsSuccess)
        {
            return Result<Order>.Failure(orderResult.Error!, orderResult.ErrorCode!);
        }

        await _orderRepository.AddAsync(orderResult.Value!, ct);
        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(orderResult.Value!);
    }
}

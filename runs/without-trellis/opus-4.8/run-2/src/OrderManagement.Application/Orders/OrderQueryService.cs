using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Orders;

public sealed class OrderQueryService
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly TimeProvider _timeProvider;

    public OrderQueryService(IOrderRepository orders, ICustomerRepository customers, TimeProvider timeProvider)
    {
        _orders = orders;
        _customers = customers;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderDto>> GetByIdAsync(IActor actor, Guid orderId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersRead))
            return Error.Forbidden($"Actor lacks required permission '{Permissions.OrdersRead}'.");

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Error.NotFound($"Order '{orderId}' was not found.");

        return order.ToDto();
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> ListByCustomerAsync(IActor actor, Guid customerId, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersReadAll))
            return Error.Forbidden($"Actor lacks required permission '{Permissions.OrdersReadAll}'.");

        var customer = await _customers.GetByIdAsync(customerId, ct);
        if (customer is null)
            return Error.NotFound($"Customer '{customerId}' was not found.");

        var orders = await _orders.ListByCustomerAsync(customerId, ct);
        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> ListOverdueAsync(IActor actor, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.OrdersReadAll))
            return Error.Forbidden($"Actor lacks required permission '{Permissions.OrdersReadAll}'.");

        var orders = await _orders.ListOverdueAsync(_timeProvider.GetUtcNow(), ct);
        return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(o => o.ToDto()).ToList());
    }
}

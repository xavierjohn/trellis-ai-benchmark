using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Queries;

public record GetOrderByIdQuery(
    Actor Actor,
    Guid OrderId) : IRequest<OrderDto>;

public class GetOrderByIdHandler(
    IOrderRepository orderRepo) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersRead))
            throw new ForbiddenException("Permission 'orders:read' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        return OrderDto.FromOrder(order);
    }
}

// ---

public record ListOrdersByCustomerQuery(
    Actor Actor,
    Guid CustomerId) : IRequest<IReadOnlyList<OrderDto>>;

public class ListOrdersByCustomerHandler(
    ICustomerRepository customerRepo,
    IOrderRepository orderRepo) : IRequestHandler<ListOrdersByCustomerQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(ListOrdersByCustomerQuery req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersReadAll))
            throw new ForbiddenException("Permission 'orders:read-all' is required.");

        var customer = await customerRepo.GetByIdAsync(req.CustomerId, ct)
            ?? throw new NotFoundException("Customer", req.CustomerId);

        var orders = await orderRepo.GetByCustomerIdAsync(customer.Id, ct);
        return orders.Select(OrderDto.FromOrder).ToList();
    }
}

// ---

public record ListOverdueOrdersQuery(
    Actor Actor,
    DateTimeOffset Now) : IRequest<IReadOnlyList<OrderDto>>;

public class ListOverdueOrdersHandler(
    IOrderRepository orderRepo) : IRequestHandler<ListOverdueOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(ListOverdueOrdersQuery req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersReadAll))
            throw new ForbiddenException("Permission 'orders:read-all' is required.");

        var cutoff = req.Now.AddDays(-7);
        var orders = await orderRepo.GetOverdueAsync(cutoff, ct);
        return orders.Select(OrderDto.FromOrder).ToList();
    }
}

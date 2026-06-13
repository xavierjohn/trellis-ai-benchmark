using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record ApproveOrderCommand(
    Actor Actor,
    Guid OrderId,
    DateTimeOffset Now) : IRequest<OrderDto>;

public class ApproveOrderHandler(
    IOrderRepository orderRepo) : IRequestHandler<ApproveOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(ApproveOrderCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersApprove))
            throw new ForbiddenException("Permission 'orders:approve' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        order.Approve(req.Now);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

// ---

public record ShipOrderCommand(
    Actor Actor,
    Guid OrderId,
    DateTimeOffset Now) : IRequest<OrderDto>;

public class ShipOrderHandler(
    IOrderRepository orderRepo) : IRequestHandler<ShipOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(ShipOrderCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersShip))
            throw new ForbiddenException("Permission 'orders:ship' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        order.Ship(req.Now);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

// ---

public record DeliverOrderCommand(
    Actor Actor,
    Guid OrderId,
    DateTimeOffset Now) : IRequest<OrderDto>;

public class DeliverOrderHandler(
    IOrderRepository orderRepo) : IRequestHandler<DeliverOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(DeliverOrderCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersDeliver))
            throw new ForbiddenException("Permission 'orders:deliver' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        order.Deliver(req.Now);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

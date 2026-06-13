using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record RemoveLineItemCommand(
    Actor Actor,
    Guid OrderId,
    Guid LineItemId) : IRequest<OrderDto>;

public class RemoveLineItemHandler(
    IOrderRepository orderRepo) : IRequestHandler<RemoveLineItemCommand, OrderDto>
{
    public async Task<OrderDto> Handle(RemoveLineItemCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersCreate))
            throw new ForbiddenException("Permission 'orders:create' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        order.RemoveLineItem(req.LineItemId);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

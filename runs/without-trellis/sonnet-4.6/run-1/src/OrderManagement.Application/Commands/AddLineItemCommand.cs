using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record AddLineItemCommand(
    Actor Actor,
    Guid OrderId,
    Guid ProductId,
    int Quantity) : IRequest<OrderDto>;

public class AddLineItemHandler(
    IOrderRepository orderRepo,
    IProductRepository productRepo) : IRequestHandler<AddLineItemCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AddLineItemCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersCreate))
            throw new ForbiddenException("Permission 'orders:create' is required.");

        if (req.Quantity < 1 || req.Quantity > 999)
            throw new ValidationException("Quantity must be between 1 and 999.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        if (order.Status != OrderStatus.Draft)
            throw new ValidationException("Line items can only be added to Draft orders.");

        var product = await productRepo.GetByIdAsync(req.ProductId, ct)
            ?? throw new NotFoundException("Product", req.ProductId);

        order.AddLineItem(product.Id, product.ProductName, req.Quantity, product.UnitPrice);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

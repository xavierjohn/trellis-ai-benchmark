using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record CancelOrderCommand(
    Actor Actor,
    Guid OrderId,
    DateTimeOffset Now) : IRequest<OrderDto>;

public class CancelOrderHandler(
    IOrderRepository orderRepo,
    IProductRepository productRepo) : IRequestHandler<CancelOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CancelOrderCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersCancel))
            throw new ForbiddenException("Permission 'orders:cancel' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        // Ownership check: must be creator OR have orders:read-all (admin)
        var isOwner = order.CreatedByActorId == req.Actor.Id;
        var isAdmin = req.Actor.HasPermission(Permissions.OrdersReadAll);
        if (!isOwner && !isAdmin)
            throw new ForbiddenException("You can only cancel orders you created.");

        var productIds = order.LineItems.Select(li => li.ProductId).ToList();
        var products = await productRepo.GetByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(p => p.Id);

        order.Cancel(req.Now, (productId, quantity) =>
        {
            if (productMap.TryGetValue(productId, out var product))
                product.ReleaseStock(quantity);
        });

        await productRepo.SaveChangesAsync(ct);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

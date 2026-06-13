using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record SubmitOrderCommand(
    Actor Actor,
    Guid OrderId,
    DateTimeOffset Now) : IRequest<OrderDto>;

public class SubmitOrderHandler(
    IOrderRepository orderRepo,
    IProductRepository productRepo) : IRequestHandler<SubmitOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(SubmitOrderCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersSubmit))
            throw new ForbiddenException("Permission 'orders:submit' is required.");

        var order = await orderRepo.GetByIdAsync(req.OrderId, ct)
            ?? throw new NotFoundException("Order", req.OrderId);

        var productIds = order.LineItems.Select(li => li.ProductId).ToList();
        var products = await productRepo.GetByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(p => p.Id);

        order.Submit(req.Now, (productId, quantity) =>
        {
            if (!productMap.TryGetValue(productId, out var product))
                throw new NotFoundException("Product", productId);
            product.ReserveStock(quantity);
        });

        await productRepo.SaveChangesAsync(ct);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

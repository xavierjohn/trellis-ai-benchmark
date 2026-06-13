using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record LineItemInput(Guid ProductId, int Quantity);

public record CreateDraftOrderCommand(
    Actor Actor,
    Guid CustomerId,
    IReadOnlyList<LineItemInput> LineItems,
    DateTimeOffset CreatedAt) : IRequest<OrderDto>;

public class CreateDraftOrderHandler(
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IOrderRepository orderRepo) : IRequestHandler<CreateDraftOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateDraftOrderCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.OrdersCreate))
            throw new ForbiddenException("Permission 'orders:create' is required.");

        if (req.LineItems is null || req.LineItems.Count == 0)
            throw new ValidationException("At least one line item is required.");

        var duplicateProductIds = req.LineItems
            .GroupBy(li => li.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProductIds.Count > 0)
            throw new ValidationException("Duplicate productIds in line items. Combine quantities instead.");

        foreach (var item in req.LineItems)
            if (item.Quantity < 1 || item.Quantity > 999)
                throw new ValidationException("Quantity must be between 1 and 999.");

        var customer = await customerRepo.GetByIdAsync(req.CustomerId, ct)
            ?? throw new NotFoundException("Customer", req.CustomerId);

        var productIds = req.LineItems.Select(li => li.ProductId).ToList();
        var products = await productRepo.GetByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var item in req.LineItems)
            if (!productMap.ContainsKey(item.ProductId))
                throw new NotFoundException("Product", item.ProductId);

        var order = Order.Create(customer.Id, req.Actor.Id, req.CreatedAt);

        foreach (var item in req.LineItems)
        {
            var product = productMap[item.ProductId];
            order.AddLineItem(product.Id, product.ProductName, item.Quantity, product.UnitPrice);
        }

        await orderRepo.AddAsync(order, ct);
        await orderRepo.SaveChangesAsync(ct);
        return OrderDto.FromOrder(order);
    }
}

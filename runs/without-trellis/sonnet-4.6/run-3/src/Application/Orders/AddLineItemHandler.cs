using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Orders;

namespace Application.Orders;

public class AddLineItemHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public AddLineItemHandler(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<Order>> HandleAsync(AddLineItemCommand command, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("orders:create"))
        {
            return Result<Order>.Forbidden("You do not have permission to modify orders.");
        }

        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);
        if (order == null)
        {
            return Result<Order>.NotFound($"Order {command.OrderId} not found.");
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
        if (product == null)
        {
            return Result<Order>.NotFound($"Product {command.ProductId} not found.");
        }

        var result = order.AddLineItem(product.ProductId, product.ProductName, command.Quantity, product.UnitPrice);
        if (!result.IsSuccess)
        {
            return Result<Order>.Failure(result.Error!, result.ErrorCode!);
        }

        await _orderRepository.SaveChangesAsync(ct);
        return Result<Order>.Success(order);
    }
}

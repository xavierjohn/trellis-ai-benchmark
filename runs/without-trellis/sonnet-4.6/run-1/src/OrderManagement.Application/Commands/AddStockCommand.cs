using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record AddStockCommand(
    Actor Actor,
    Guid ProductId,
    int Quantity) : IRequest<ProductDto>;

public class AddStockHandler(
    IProductRepository productRepo) : IRequestHandler<AddStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AddStockCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.ProductsManageStock))
            throw new ForbiddenException("Permission 'products:manage-stock' is required.");

        var product = await productRepo.GetByIdAsync(req.ProductId, ct)
            ?? throw new NotFoundException("Product", req.ProductId);

        product.AddStock(req.Quantity);
        await productRepo.SaveChangesAsync(ct);
        return ProductDto.FromProduct(product);
    }
}

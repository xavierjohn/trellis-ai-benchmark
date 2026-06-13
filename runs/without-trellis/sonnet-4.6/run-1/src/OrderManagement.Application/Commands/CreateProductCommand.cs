using MediatR;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Aggregates;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Commands;

public record CreateProductCommand(
    Actor Actor,
    string ProductName,
    string SKU,
    decimal UnitPrice) : IRequest<ProductDto>;

public class CreateProductHandler(
    IProductRepository productRepo) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand req, CancellationToken ct)
    {
        if (!req.Actor.HasPermission(Permissions.ProductsCreate))
            throw new ForbiddenException("Permission 'products:create' is required.");

        var existing = await productRepo.GetBySkuAsync(req.SKU, ct);
        if (existing is not null)
            throw new ConflictException($"A product with SKU '{req.SKU}' already exists.");

        var product = Product.Create(req.ProductName, req.SKU, req.UnitPrice);
        await productRepo.AddAsync(product, ct);
        await productRepo.SaveChangesAsync(ct);
        return ProductDto.FromProduct(product);
    }
}

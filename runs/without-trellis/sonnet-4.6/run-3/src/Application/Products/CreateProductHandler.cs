using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Products;

namespace Application.Products;

public class CreateProductHandler
{
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<Product>> HandleAsync(CreateProductCommand command, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("products:create"))
        {
            return Result<Product>.Forbidden("You do not have permission to create products.");
        }

        var productResult = Product.Create(command.ProductName, command.SKU, command.UnitPrice, command.StockQuantity);
        if (!productResult.IsSuccess)
        {
            return Result<Product>.Failure(productResult.Error!, productResult.ErrorCode!);
        }

        if (await _productRepository.SkuExistsAsync(productResult.Value!.SKU, ct))
        {
            return Result<Product>.Conflict($"A product with SKU '{command.SKU}' already exists.");
        }

        await _productRepository.AddAsync(productResult.Value, ct);
        await _productRepository.SaveChangesAsync(ct);
        return Result<Product>.Success(productResult.Value);
    }
}

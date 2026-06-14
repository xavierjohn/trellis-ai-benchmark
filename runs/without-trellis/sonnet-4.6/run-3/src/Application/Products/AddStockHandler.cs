using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Products;

namespace Application.Products;

public class AddStockHandler
{
    private readonly IProductRepository _productRepository;

    public AddStockHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<Product>> HandleAsync(AddStockCommand command, Actor actor, CancellationToken ct = default)
    {
        if (!actor.HasPermission("products:manage-stock"))
        {
            return Result<Product>.Forbidden("You do not have permission to manage stock.");
        }

        var product = await _productRepository.GetByIdAsync(command.ProductId, ct);
        if (product == null)
        {
            return Result<Product>.NotFound($"Product {command.ProductId} not found.");
        }

        var result = product.AddStock(command.Quantity);
        if (!result.IsSuccess)
        {
            return Result<Product>.Failure(result.Error!, result.ErrorCode!);
        }

        await _productRepository.SaveChangesAsync(ct);
        return Result<Product>.Success(product);
    }
}

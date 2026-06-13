using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Products;

public sealed class ProductService(IProductRepository products, IUnitOfWork unitOfWork)
{
    public async Task<Result<ProductResponse>> CreateAsync(Actor actor, CreateProductRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.ProductsCreate))
            return Error.Forbidden($"Actor '{actor.Id}' lacks permission '{Permissions.ProductsCreate}'.");

        var productResult = Product.Create(request.ProductName, request.Sku, request.UnitPrice, request.InitialStock);
        if (productResult.IsFailure)
            return productResult.Error!;

        var product = productResult.Value;

        if (await products.SkuExistsAsync(product.Sku, ct))
            return Error.Conflict($"A product with SKU '{product.Sku}' already exists.");

        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return product.ToResponse();
    }

    public async Task<Result<ProductResponse>> AddStockAsync(Actor actor, Guid productId, AddStockRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.ProductsManageStock))
            return Error.Forbidden($"Actor '{actor.Id}' lacks permission '{Permissions.ProductsManageStock}'.");

        var product = await products.GetByIdAsync(productId, ct);
        if (product is null)
            return Error.NotFound($"Product '{productId}' was not found.");

        var addResult = product.AddStock(request.Quantity);
        if (addResult.IsFailure)
            return addResult.Error!;

        await unitOfWork.SaveChangesAsync(ct);
        return product.ToResponse();
    }
}

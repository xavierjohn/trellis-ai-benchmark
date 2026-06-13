using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Products;

public sealed class ProductService
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> CreateAsync(IActor actor, CreateProductRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.ProductsCreate))
            return Error.Forbidden($"Actor lacks required permission '{Permissions.ProductsCreate}'.");

        var productResult = Product.Create(request.ProductName, request.Sku, request.UnitPrice);
        if (productResult.IsFailure)
            return productResult.Error!;

        var product = productResult.Value;

        if (await _products.ExistsBySkuAsync(product.Sku, ct))
            return Error.Conflict($"A product with SKU '{product.Sku}' already exists.");

        await _products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return product.ToDto();
    }

    public async Task<Result<ProductDto>> AddStockAsync(IActor actor, Guid productId, AddStockRequest request, CancellationToken ct = default)
    {
        if (!actor.Has(Permissions.ProductsManageStock))
            return Error.Forbidden($"Actor lacks required permission '{Permissions.ProductsManageStock}'.");

        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null)
            return Error.NotFound($"Product '{productId}' was not found.");

        var addResult = product.AddStock(request.Quantity);
        if (addResult.IsFailure)
            return addResult.Error!;

        await _unitOfWork.SaveChangesAsync(ct);
        return product.ToDto();
    }
}

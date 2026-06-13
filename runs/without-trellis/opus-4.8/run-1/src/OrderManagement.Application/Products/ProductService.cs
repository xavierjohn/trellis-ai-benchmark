using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Authorization;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Products;

namespace OrderManagement.Application.Products;

public sealed class ProductService
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IActorProvider _actorProvider;

    public ProductService(IProductRepository products, IUnitOfWork uow, IActorProvider actorProvider)
    {
        _products = products;
        _uow = uow;
        _actorProvider = actorProvider;
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.ProductsCreate);
        if (auth.IsFailure) return auth.Error!;

        var productResult = Product.Create(request.ProductName, request.Sku, request.UnitPrice);
        if (productResult.IsFailure) return productResult.Error!;

        var product = productResult.Value;

        if (await _products.ExistsBySkuAsync(product.Sku.Value, ct))
            return Error.Conflict($"A product with SKU '{product.Sku.Value}' already exists.");

        await _products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        return ProductResponse.From(product);
    }

    public async Task<Result<ProductResponse>> AddStockAsync(Guid productId, AddStockRequest request, CancellationToken ct = default)
    {
        var auth = AuthorizationGuard.Require(_actorProvider.Current, Permissions.ProductsManageStock);
        if (auth.IsFailure) return auth.Error!;

        var product = await _products.GetByIdAsync(productId, ct);
        if (product is null)
            return Error.NotFound($"Product '{productId}' was not found.");

        var addResult = product.AddStock(request.Quantity);
        if (addResult.IsFailure) return addResult.Error!;

        await _uow.SaveChangesAsync(ct);
        return ProductResponse.From(product);
    }
}

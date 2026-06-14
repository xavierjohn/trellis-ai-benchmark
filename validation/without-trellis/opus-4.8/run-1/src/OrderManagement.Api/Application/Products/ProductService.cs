using OrderManagement.Api.Application.Abstractions;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Api.Contracts;
using OrderManagement.Api.Domain.Common;
using OrderManagement.Api.Domain.Products;

namespace OrderManagement.Api.Application.Products;

public sealed class ProductService
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public ProductService(IProductRepository products, IUnitOfWork uow)
    {
        _products = products;
        _uow = uow;
    }

    public async Task<Product> CreateAsync(Actor actor, CreateProductRequest request, CancellationToken ct = default)
    {
        actor.Require(Permissions.ProductsCreate);

        var product = Product.Create(
            request.ProductName ?? string.Empty,
            request.Sku ?? string.Empty,
            request.UnitPrice);

        if (await _products.ExistsBySkuAsync(product.Sku, ct))
            throw new ConflictAppException($"A product with SKU '{product.Sku}' already exists.");

        await _products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);
        return product;
    }

    public async Task<Product> AddStockAsync(Actor actor, Guid productId, AddStockRequest request, CancellationToken ct = default)
    {
        actor.Require(Permissions.ProductsManageStock);

        var product = await _products.GetByIdAsync(productId, ct)
            ?? throw new NotFoundAppException($"Product '{productId}' was not found.");

        product.AddStock(request.Quantity);
        await _uow.SaveChangesAsync(ct);
        return product;
    }
}

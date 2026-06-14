namespace OrderManagement.Application.Products;

using OrderManagement.Domain;

/// <summary>
/// Product persistence port.
/// </summary>
public interface IProductRepository
{
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);

    Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken);

    void Add(Product product);
}

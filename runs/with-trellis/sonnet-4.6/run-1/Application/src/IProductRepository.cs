namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>
/// Product repository contract.
/// </summary>
public interface IProductRepository
{
    /// <summary>Finds a product by identifier.</summary>
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Finds a product by SKU.</summary>
    Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken);

    /// <summary>Loads products by identifier.</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken);

    /// <summary>Stages a product for insertion.</summary>
    void Add(Product product);
}

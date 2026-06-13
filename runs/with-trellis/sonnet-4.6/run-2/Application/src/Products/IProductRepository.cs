namespace OrderManagement.Application.Products;

using OrderManagement.Domain;

/// <summary>Repository for product aggregates.</summary>
public interface IProductRepository
{
    /// <summary>Finds a product by its unique identifier.</summary>
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Finds a product by its SKU.</summary>
    Task<Maybe<Product>> FindBySkuAsync(SKU sku, CancellationToken cancellationToken);

    /// <summary>Adds a new product to the repository.</summary>
    void Add(Product product);
}

namespace OrderManagement.Application.Abstractions;

using OrderManagement.Domain;

/// <summary>Persistence operations for the <see cref="Product"/> aggregate.</summary>
public interface IProductRepository
{
    /// <summary>Finds a product by ID.</summary>
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Loads all products whose IDs are in the supplied set.</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);

    /// <summary>Returns true when a product with the given SKU already exists.</summary>
    Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken);

    /// <summary>Stages a new product for insertion.</summary>
    void Add(Product product);
}

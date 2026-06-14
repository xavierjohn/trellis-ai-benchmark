namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>Persistence contract for the <see cref="Product"/> aggregate.</summary>
public interface IProductRepository
{
    /// <summary>Finds a product by ID, or <see cref="Maybe{T}.None"/> if absent.</summary>
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Finds a product by SKU, or <see cref="Maybe{T}.None"/> if absent.</summary>
    Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken);

    /// <summary>Loads every product whose ID is in <paramref name="ids"/>.</summary>
    Task<IReadOnlyList<Product>> FindManyByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);

    /// <summary>Stages a product for insertion. The unit-of-work commits on handler success.</summary>
    void Add(Product product);
}

namespace OrderManagement.Application;

using OrderManagement.Domain;

/// <summary>Persistence contract for the <see cref="Product"/> aggregate.</summary>
public interface IProductRepository
{
    /// <summary>Finds a product by id, or <see cref="Maybe{T}.None"/> when absent.</summary>
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Loads every product matching one of the supplied ids (missing ids are omitted).</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken);

    /// <summary>Stages a new product for insertion. The unit of work commits on success.</summary>
    void Add(Product product);
}

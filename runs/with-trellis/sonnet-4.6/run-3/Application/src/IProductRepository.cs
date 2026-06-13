namespace OrderManagement.Application;

using OrderManagement.Domain;
using Trellis;

/// <summary>Repository interface for Product persistence.</summary>
public interface IProductRepository
{
    Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> FindByIdsAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken);
    Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken);
    void Add(Product product);
}

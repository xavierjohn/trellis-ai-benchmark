namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Product repository implementation.
/// </summary>
internal sealed class ProductRepository(AppDbContext context) : RepositoryBase<Product, ProductId>(context), IProductRepository
{
    /// <inheritdoc />
    public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        DbSet.Where(product => product.Sku == sku).FirstOrDefaultMaybeAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        return await DbSet.Where(product => idList.Contains(product.Id)).ToListAsync(cancellationToken);
    }
}

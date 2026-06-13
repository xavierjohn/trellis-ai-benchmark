namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

internal class ProductRepository(AppDbContext context) : RepositoryBase<Product, ProductId>(context), IProductRepository
{
    public Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        DbSet.AnyAsync(product => product.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Product>> FindByIdsAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        return await DbSet.Where(product => idList.Contains(product.Id)).ToListAsync(cancellationToken);
    }
}

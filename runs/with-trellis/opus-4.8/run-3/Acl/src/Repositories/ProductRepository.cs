namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
internal sealed class ProductRepository : RepositoryBase<Product, ProductId>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        DbSet.FirstOrDefaultMaybeAsync(p => p.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Product>> FindManyByIdsAsync(
        IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        await DbSet.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
}

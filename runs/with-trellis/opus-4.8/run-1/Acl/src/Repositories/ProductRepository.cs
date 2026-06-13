namespace OrderManagement.AntiCorruptionLayer.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
public sealed class ProductRepository(AppDbContext context)
    : RepositoryBase<Product, ProductId>(context), IProductRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<ProductId> ids,
        CancellationToken cancellationToken) =>
        await DbSet.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        DbSet.AnyAsync(p => p.Sku == sku, cancellationToken);
}

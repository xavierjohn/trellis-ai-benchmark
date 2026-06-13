namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core repository for the <see cref="Product"/> aggregate.</summary>
public sealed class ProductRepository(AppDbContext context)
    : RepositoryBase<Product, ProductId>(context), IProductRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        return await DbSet.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
    }
}

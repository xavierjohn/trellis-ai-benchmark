namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core product repository.
/// </summary>
internal sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultMaybeAsync(product => product.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken) =>
        await context.Products
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);

    public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultMaybeAsync(product => product.Sku == sku, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
}

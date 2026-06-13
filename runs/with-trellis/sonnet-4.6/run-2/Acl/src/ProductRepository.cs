namespace OrderManagement.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>EF Core implementation of IProductRepository.</summary>
internal class ProductRepository : RepositoryBase<Product, ProductId>, IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Maybe<Product>> FindBySkuAsync(SKU sku, CancellationToken cancellationToken) =>
        await _context.Products
            .Where(p => p.SKU == sku)
            .FirstOrDefaultMaybeAsync(cancellationToken);
}

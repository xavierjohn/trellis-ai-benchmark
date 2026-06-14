using Application.Interfaces;
using Domain.Products;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Products.FirstOrDefaultAsync(p => p.ProductId == id, ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default) =>
        _context.Products.AnyAsync(p => p.SKU == sku.ToUpperInvariant(), ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
    }

    public Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}

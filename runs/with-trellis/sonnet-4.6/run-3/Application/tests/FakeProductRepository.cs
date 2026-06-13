namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Testing;

internal class FakeProductRepository : IProductRepository
{
    private readonly FakeRepository<Product, ProductId> _repo;

    public FakeProductRepository(FakeRepository<Product, ProductId> repo) => _repo = repo;

    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Product>> FindByIdsAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken)
    {
        var idSet = ids.ToHashSet();
        IReadOnlyList<Product> result = _repo.GetAll().Where(p => idSet.Contains(p.Id)).ToList();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken) =>
        Task.FromResult(_repo.GetAll().Any(p => p.Sku == sku));

    public void Add(Product product) => _repo.Add(product);
}

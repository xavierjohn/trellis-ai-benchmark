namespace Application.Tests;

using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Testing;

internal sealed class FakeProductRepository(FakeRepository<Product, ProductId> repository) : IProductRepository
{
    public Task<Maybe<Product>> FindByIdAsync(ProductId id, CancellationToken cancellationToken) => repository.FindByIdAsync(id, cancellationToken);

    public Task<Maybe<Product>> FindBySkuAsync(Sku sku, CancellationToken cancellationToken)
    {
        var product = repository.GetAll().FirstOrDefault(existing => existing.Sku == sku);
        return Task.FromResult(product is null ? Maybe<Product>.None : Maybe.From(product));
    }

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken)
    {
        var idSet = ids.ToHashSet();
        IReadOnlyList<Product> products = repository.GetAll().Where(product => idSet.Contains(product.Id)).ToList();
        return Task.FromResult(products);
    }

    public void Add(Product product) => repository.Add(product);
}

namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Creates a new product.</summary>
public sealed record CreateProductCommand(
    ProductName ProductName,
    Sku Sku,
    UnitPrice UnitPrice) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

/// <summary>Handler for <see cref="CreateProductCommand"/>.</summary>
internal sealed class CreateProductCommandHandler(
    IProductRepository repository) : ICommandHandler<CreateProductCommand, Result<Product>>
{
    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (await repository.ExistsBySkuAsync(command.Sku, cancellationToken))
        {
            return Result.Fail<Product>(
                new Error.Conflict(ResourceRef.For<Product>(), "sku.duplicate")
                {
                    Detail = "A product with this SKU already exists.",
                });
        }

        var product = new Product(command.ProductName, command.Sku, command.UnitPrice);
        repository.Add(product);
        return Result.Ok(product);
    }
}

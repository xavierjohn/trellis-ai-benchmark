namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Creates a new product.</summary>
public sealed record CreateProductCommand(ProductName Name, Sku Sku, UnitPrice UnitPrice)
    : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.ProductsCreate];
}

/// <summary>Handles <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler(IProductRepository repository)
    : ICommandHandler<CreateProductCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (await repository.ExistsBySkuAsync(command.Sku, cancellationToken))
            return Result.Fail<Product>(new Error.Conflict(
                ResourceRef.For<Product>(command.Sku.Value), "duplicate_sku")
            {
                Detail = "A product with this SKU already exists.",
            });

        var product = new Product(command.Name, command.Sku, command.UnitPrice);
        repository.Add(product);
        return Result.Ok(product);
    }
}

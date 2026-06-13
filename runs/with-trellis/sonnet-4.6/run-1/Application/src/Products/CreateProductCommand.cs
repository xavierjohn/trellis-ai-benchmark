namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Creates a product.
/// </summary>
public sealed record CreateProductCommand(ProductName Name, Sku Sku, decimal UnitPrice) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

/// <summary>
/// Handles <see cref="CreateProductCommand"/>.
/// </summary>
public sealed class CreateProductCommandHandler(IProductRepository repository) : ICommandHandler<CreateProductCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (command.UnitPrice <= 0)
            return Result.Fail<Product>(Error.InvalidInput.ForField("unitPrice", "positive", "Unit price must be greater than zero."));

        var existing = await repository.FindBySkuAsync(command.Sku, cancellationToken);
        if (existing.HasValue)
        {
            return Result.Fail<Product>(new Error.Conflict(ResourceRef.For<Product>(command.Sku.Value), "product.duplicate_sku")
            {
                Detail = "A product with this SKU already exists.",
            });
        }

        var product = new Product(command.Name, command.Sku, command.UnitPrice);
        repository.Add(product);
        return Result.Ok(product);
    }
}

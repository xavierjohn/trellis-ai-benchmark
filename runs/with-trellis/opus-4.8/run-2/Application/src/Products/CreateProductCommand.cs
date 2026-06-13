namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

/// <summary>Creates a new product.</summary>
public sealed record CreateProductCommand(ProductName Name, Sku Sku, MonetaryAmount UnitPrice)
    : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.ProductsCreate];
}

/// <summary>Handles <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler(IProductRepository products)
    : ICommandHandler<CreateProductCommand, Result<Product>>
{
    /// <inheritdoc />
    public ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken) =>
        Product.TryCreate(command.Name, command.Sku, command.UnitPrice)
            .Tap(products.Add)
            .AsValueTask();
}

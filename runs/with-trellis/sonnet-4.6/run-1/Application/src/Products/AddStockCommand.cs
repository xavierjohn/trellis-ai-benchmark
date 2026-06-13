namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>
/// Adds stock to an existing product.
/// </summary>
public sealed record AddStockCommand(ProductId ProductId, int Quantity) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

/// <summary>
/// Handles <see cref="AddStockCommand"/>.
/// </summary>
public sealed class AddStockCommandHandler(IProductRepository repository) : ICommandHandler<AddStockCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        var maybeProduct = await repository.FindByIdAsync(command.ProductId, cancellationToken);
        if (!maybeProduct.TryGetValue(out var product))
        {
            return Result.Fail<Product>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId))
            {
                Detail = $"Product {command.ProductId} not found.",
            });
        }

        return product.AddStock(command.Quantity).Map(_ => product);
    }
}

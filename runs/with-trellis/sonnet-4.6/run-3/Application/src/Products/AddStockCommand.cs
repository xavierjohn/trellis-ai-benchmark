namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis;
using Trellis.Authorization;

/// <summary>Adds stock to a product.</summary>
public sealed record AddStockCommand(
    ProductId ProductId,
    int Quantity) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

/// <summary>Handler for <see cref="AddStockCommand"/>.</summary>
internal sealed class AddStockCommandHandler(
    IProductRepository repository) : ICommandHandler<AddStockCommand, Result<Product>>
{
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        var product = await repository.FindByIdAsync(command.ProductId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId.Value} not found." });

        return product.Bind(foundProduct => foundProduct.AddStock(command.Quantity));
    }
}

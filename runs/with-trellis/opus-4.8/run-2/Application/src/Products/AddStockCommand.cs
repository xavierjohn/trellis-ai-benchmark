namespace OrderManagement.Application;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Adds stock to an existing product.</summary>
public sealed record AddStockCommand(ProductId ProductId, StockAddition Quantity)
    : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.ProductsManageStock];
}

/// <summary>Handles <see cref="AddStockCommand"/>.</summary>
public sealed class AddStockCommandHandler(IProductRepository products)
    : ICommandHandler<AddStockCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        var maybe = await products.FindByIdAsync(command.ProductId, cancellationToken);
        return maybe
            .ToResult(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = "Product not found." })
            .Bind(product => product.AddStock(command.Quantity));
    }
}

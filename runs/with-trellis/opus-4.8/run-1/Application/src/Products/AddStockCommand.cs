namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Adds stock to a product.</summary>
public sealed record AddStockCommand(ProductId ProductId, int Quantity)
    : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions => [Permissions.ProductsManageStock];
}

/// <summary>Handles <see cref="AddStockCommand"/>.</summary>
public sealed class AddStockCommandHandler(IProductRepository repository)
    : ICommandHandler<AddStockCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        var maybe = await repository.FindByIdAsync(command.ProductId, cancellationToken);
        return maybe
            .ToResult(new Error.NotFound(ResourceRef.For<Product>(command.ProductId.Value))
            {
                Detail = "Product not found.",
            })
            .Bind(product => product.AddStock(command.Quantity));
    }
}

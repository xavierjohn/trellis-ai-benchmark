namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;

/// <summary>
/// Creates a product.
/// </summary>
public sealed record CreateProductCommand(ProductName ProductName, Sku Sku, MonetaryAmount UnitPrice)
    : ICommand<Result<Product>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

/// <summary>
/// Handles product creation.
/// </summary>
public sealed class CreateProductCommandHandler(IProductRepository repository) : ICommandHandler<CreateProductCommand, Result<Product>>
{
    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var existing = await repository.FindBySkuAsync(command.Sku, cancellationToken);
        if (existing.HasValue)
        {
            return Result.Fail<Product>(new Error.Conflict(ResourceRef.For<Product>(), "duplicate_sku")
            {
                Detail = "A product with this SKU already exists.",
            });
        }

        return Product.TryCreate(command.ProductName, command.Sku, command.UnitPrice)
            .Tap(repository.Add);
    }
}

/// <summary>
/// Adds available stock to a product.
/// </summary>
public sealed record AddStockCommand(ProductId ProductId, StockAdjustmentQuantity Quantity)
    : ICommand<Result<Product>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

/// <summary>
/// Handles stock additions.
/// </summary>
public sealed class AddStockCommandHandler(IProductRepository repository) : ICommandHandler<AddStockCommand, Result<Product>>
{
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken) =>
        await repository.FindByIdAsync(command.ProductId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId} not found." })
            .BindAsync(product => product.AddStock(command.Quantity));
}

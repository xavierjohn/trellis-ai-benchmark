namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Create product command.</summary>
public sealed record CreateProductCommand(ProductName Name, Sku Sku, UnitPrice UnitPrice)
    : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

/// <summary>Add stock command.</summary>
public sealed record AddStockCommand(ProductId ProductId, StockAdjustmentQuantity Quantity)
    : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

/// <summary>Create product handler.</summary>
public sealed class CreateProductCommandHandler(IProductRepository products)
    : ICommandHandler<CreateProductCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var duplicate = await products.FindBySkuAsync(command.Sku, cancellationToken);
        if (duplicate.HasValue)
            return Result.Fail<Product>(new Error.Conflict(ResourceRef.For<Product>(), "product.sku.duplicate") { Detail = "A product with this SKU already exists." });

        return Product.TryCreate(command.Name, command.Sku, command.UnitPrice)
            .Tap(products.Add);
    }
}

/// <summary>Add stock handler.</summary>
public sealed class AddStockCommandHandler(IProductRepository products)
    : ICommandHandler<AddStockCommand, Result<Product>>
{
    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        var productResult = await products.FindByIdAsync(command.ProductId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId.Value} not found." });

        return productResult.Bind(product => product.AddStock(command.Quantity));
    }
}

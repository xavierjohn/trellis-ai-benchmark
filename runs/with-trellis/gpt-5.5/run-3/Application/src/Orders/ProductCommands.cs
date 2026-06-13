namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

public sealed record CreateProductCommand(ProductName ProductName, Sku Sku, UnitPrice UnitPrice)
    : ICommand<Result<Product>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<Product>>
{
    private readonly IProductRepository _products;
    private readonly TimeProvider _timeProvider;

    public CreateProductCommandHandler(IProductRepository products, TimeProvider timeProvider)
    {
        _products = products;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (await _products.ExistsBySkuAsync(command.Sku, cancellationToken))
        {
            return Result.Fail<Product>(new Error.Conflict(ResourceRef.For<Product>(), "products.duplicate_sku")
            {
                Detail = "A product with this SKU already exists.",
            });
        }

        var product = new Product(command.ProductName, command.Sku, command.UnitPrice, _timeProvider);
        _products.Add(product);
        return Result.Ok(product);
    }
}

public sealed record AddStockCommand(ProductId ProductId, StockAdjustmentQuantity Quantity)
    : ICommand<Result<Product>>, IAuthorize
{
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

public sealed class AddStockCommandHandler : ICommandHandler<AddStockCommand, Result<Product>>
{
    private readonly IProductRepository _products;

    public AddStockCommandHandler(IProductRepository products) => _products = products;

    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken) =>
        (await _products.FindByIdAsync(command.ProductId, cancellationToken))
            .ToResult(NotFound.Product(command.ProductId))
            .Bind(product => product.AddStock(command.Quantity));
}

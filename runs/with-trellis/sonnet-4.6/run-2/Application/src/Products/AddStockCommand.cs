namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Adds stock to a product.</summary>
public sealed record AddStockCommand(ProductId ProductId, Quantity Quantity) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

/// <summary>Handler for AddStockCommand.</summary>
public sealed class AddStockCommandHandler : ICommandHandler<AddStockCommand, Result<Product>>
{
    private readonly IProductRepository _repository;

    public AddStockCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken)
    {
        var productMaybe = await _repository.FindByIdAsync(command.ProductId, cancellationToken);
        if (productMaybe.HasNoValue)
            return Result.Fail<Product>(new Error.NotFound(ResourceRef.For<Product>(command.ProductId)) { Detail = $"Product {command.ProductId} not found." });

        var product = productMaybe.GetValueOrThrow();
        var addResult = product.AddStock(command.Quantity);
        if (addResult.IsFailure)
            return Result.Fail<Product>(addResult.Error);

        return Result.Ok(product);
    }
}

namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Adds stock to an existing product.</summary>
public sealed record AddStockCommand(ProductId ProductId, int Quantity) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsManageStock];
}

/// <summary>Handler for <see cref="AddStockCommand"/>.</summary>
public sealed class AddStockCommandHandler : ICommandHandler<AddStockCommand, Result<Product>>
{
    private readonly IProductRepository _repository;

    /// <summary>Creates the handler.</summary>
    public AddStockCommandHandler(IProductRepository repository) => _repository = repository;

    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(AddStockCommand command, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(command.ProductId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Product>(command.ProductId))
            {
                Detail = $"Product {command.ProductId.Value} not found.",
            })
            .BindAsync(product => product.AddStock(command.Quantity));
}

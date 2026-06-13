namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Creates a new product.</summary>
public sealed record CreateProductCommand(
    ProductName ProductName,
    SKU SKU,
    UnitPrice UnitPrice) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

/// <summary>Handler for CreateProductCommand.</summary>
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<Product>>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var existing = await _repository.FindBySkuAsync(command.SKU, cancellationToken);
        if (existing.HasValue)
            return Result.Fail<Product>(
                new Error.Conflict(ResourceRef.For<Product>(command.SKU.Value), "product.duplicate.sku")
                { Detail = $"A product with SKU '{command.SKU.Value}' already exists." });

        var product = new Product(command.ProductName, command.SKU, command.UnitPrice);
        _repository.Add(product);
        return Result.Ok(product);
    }
}

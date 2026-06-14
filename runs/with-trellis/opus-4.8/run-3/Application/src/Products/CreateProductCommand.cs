namespace OrderManagement.Application.Products;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Creates a new product. SKU must be unique.</summary>
public sealed record CreateProductCommand(
    ProductName Name,
    Sku Sku,
    UnitPrice UnitPrice) : ICommand<Result<Product>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.ProductsCreate];
}

/// <summary>Handler for <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<Product>>
{
    private readonly IProductRepository _repository;

    /// <summary>Creates the handler.</summary>
    public CreateProductCommandHandler(IProductRepository repository) => _repository = repository;

    /// <inheritdoc />
    public async ValueTask<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var existing = await _repository.FindBySkuAsync(command.Sku, cancellationToken);
        if (existing.HasValue)
            return Result.Fail<Product>(new Error.Conflict(
                ResourceRef.For<Product>(command.Sku.Value), "product.duplicate_sku")
            {
                Detail = $"A product with SKU '{command.Sku.Value}' already exists.",
            });

        var product = Product.Create(command.Name, command.Sku, command.UnitPrice);
        _repository.Add(product);
        return Result.Ok(product);
    }
}

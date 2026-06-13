using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Auth;
using OrderManagement.Api.Domain;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Services;

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<AddStockRequest> _stockValidator;

    public ProductService(
        AppDbContext db,
        IValidator<CreateProductRequest> createValidator,
        IValidator<AddStockRequest> stockValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _stockValidator = stockValidator;
    }

    public async Task<Product> CreateProduct(Actor actor, CreateProductRequest request)
    {
        actor.RequirePermission(PermissionConstants.ProductsCreate);

        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.Select(e => e.ErrorMessage));

        var exists = await _db.Products.AnyAsync(p => p.SKU == request.Sku);
        if (exists)
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");

        var product = Product.Create(request.ProductName, request.Sku, request.UnitPrice);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return product;
    }

    public async Task<Product> AddStock(Actor actor, Guid productId, AddStockRequest request)
    {
        actor.RequirePermission(PermissionConstants.ProductsManageStock);

        var validation = await _stockValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.Select(e => e.ErrorMessage));

        var product = await _db.Products.FindAsync(productId)
            ?? throw new NotFoundException($"Product {productId} not found.");

        product.AddStock(request.Quantity);
        await _db.SaveChangesAsync();

        return product;
    }
}

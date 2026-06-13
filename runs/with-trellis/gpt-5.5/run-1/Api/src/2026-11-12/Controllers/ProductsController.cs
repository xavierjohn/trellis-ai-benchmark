namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Domain;
using Trellis;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Product endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>Constructor.</summary>
    public ProductsController(AppDbContext db) => _db = db;

    /// <summary>Create a product.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.ProductsCreate) is { } forbidden)
            return forbidden;

        var result = Product.TryCreate(request.ProductName, request.Sku, request.UnitPrice);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        if (!result.TryGetValue(out var product))
            return ApiSupport.Problem(this, result.Error!);

        if (await _db.Products.AnyAsync(p => p.Sku == product.Sku, cancellationToken))
            return ApiSupport.Problem(this, new Error.Conflict(ResourceRef.For("Product", product.Sku), "duplicate.sku") { Detail = "A product with this SKU already exists." });

        _db.Products.Add(product);
        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);

        return Created($"/api/products/{product.Id}?api-version=2026-11-12", ProductResponse.From(product));
    }

    /// <summary>Add stock to a product.</summary>
    [HttpPost("{id:guid}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> AddStock(Guid id, AddStockRequest request, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.ProductsManageStock) is { } forbidden)
            return forbidden;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
            return ApiSupport.Problem(this, new Error.NotFound(ResourceRef.For("Product", id)) { Detail = "Product not found." });

        var result = product.AddStock(request.Quantity);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);
        return ProductResponse.From(product);
    }
}

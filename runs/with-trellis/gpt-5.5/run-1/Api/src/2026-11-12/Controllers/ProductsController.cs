namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Asp;

/// <summary>
/// Product endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a product.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken) =>
        sender.Send(new CreateProductCommand(request.ProductName, request.Sku, request.UnitPrice), cancellationToken)
            .ToHttpResponseAsync(
                ProductResponse.From,
                options => options.Created(product => $"/api/products/{product.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<ProductResponse>();

    /// <summary>
    /// Adds product stock.
    /// </summary>
    [HttpPost("{id}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<ProductResponse>> AddStock(
        ProductId id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken) =>
        sender.Send(new AddStockCommand(id, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();
}

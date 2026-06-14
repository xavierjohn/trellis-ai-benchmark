namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Asp;

/// <summary>Product endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>Creates a new product.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(new CreateProductCommand(request.Name, request.Sku, request.UnitPrice), cancellationToken)
            .ToHttpResponseAsync(
                ProductResponse.From,
                opts => opts.Created(p => $"/api/products/{p.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<ProductResponse>();

    /// <summary>Adds stock to an existing product.</summary>
    [HttpPost("{id}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<ProductResponse>> AddStock(
        ProductId id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(new AddStockCommand(id, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();
}

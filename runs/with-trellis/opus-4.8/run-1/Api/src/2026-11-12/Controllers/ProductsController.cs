namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;

/// <summary>Product endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    private const string ApiVersion = "2026-11-12";

    /// <summary>Creates a new product.</summary>
    /// <param name="request">The product to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateProductCommand(request.Name, request.Sku, request.UnitPrice), cancellationToken);

        return result
            .ToHttpResponse(
                body: ProductResponse.From,
                configure: opts => opts.Created(p => $"/api/products/{p.Id.Value}?api-version={ApiVersion}"))
            .AsActionResult<ProductResponse>();
    }

    /// <summary>Adds stock to a product.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="request">Quantity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> AddStock(
        ProductId id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddStockCommand(id, request.Quantity), cancellationToken);
        return result
            .ToHttpResponse(body: ProductResponse.From)
            .AsActionResult<ProductResponse>();
    }
}

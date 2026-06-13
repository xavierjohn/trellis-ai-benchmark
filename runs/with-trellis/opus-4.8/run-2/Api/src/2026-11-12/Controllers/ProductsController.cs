namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Primitives;

/// <summary>Product endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    /// <summary>Creates a new product.</summary>
    /// <param name="request">The product to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created product.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = Result.Combine(
                ProductName.TryCreate(request.Name, "name"),
                Sku.TryCreate(request.Sku, "sku"),
                MonetaryAmount.TryCreate(request.UnitPrice, "unitPrice"))
            .Map(t => new CreateProductCommand(t.Item1, t.Item2, t.Item3));

        return await command
            .BindAsync(c => sender.Send(c, cancellationToken))
            .ToHttpResponseAsync(
                ProductResponse.From,
                opts => opts.Created(p => $"/api/products/{p.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<ProductResponse>();
    }

    /// <summary>Adds stock to a product.</summary>
    /// <param name="id">Product id.</param>
    /// <param name="request">The stock addition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated product.</returns>
    [HttpPost("{id:guid}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ProductResponse>> AddStock(
        ProductId id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken) =>
        await StockAddition.TryCreate(request.Quantity, "quantity")
            .Map(quantity => new AddStockCommand(id, quantity))
            .BindAsync(c => sender.Send(c, cancellationToken))
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();
}

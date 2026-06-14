namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>Product endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    /// <summary>Create a product.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken) =>
        sender.Send(new CreateProductCommand(request.Name, request.Sku, request.UnitPrice), cancellationToken)
            .ToHttpResponseAsync(
                ProductResponse.From,
                opts => opts
                    .Created(product => $"/api/products/{((Guid)product.Id).ToString()}?api-version=2026-11-12")
                    .WithVersionedRoute())
            .AsActionResultAsync<ProductResponse>();

    /// <summary>Add product stock.</summary>
    [HttpPost("{id}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<ProductResponse>> AddStock(ProductId id, [FromBody] AddStockRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddStockCommand(id, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();
}

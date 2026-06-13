namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>
/// Products controller.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Constructor.</summary>
    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create a new product.
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
        _sender.Send(new CreateProductCommand(request.ProductName, request.SKU, request.UnitPrice), cancellationToken)
            .ToHttpResponseAsync(
                ProductResponse.From,
                opts => opts
                    .CreatedAtRoute("Products_GetById", p => new Microsoft.AspNetCore.Routing.RouteValueDictionary
                    {
                        ["id"] = (Guid)p.Id
                    })
                    .WithVersionedRoute())
            .AsActionResultAsync<ProductResponse>();

    /// <summary>
    /// Get a product by ID.
    /// </summary>
    [HttpGet("{id}", Name = "Products_GetById")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<ProductResponse>> GetById(ProductId id, CancellationToken cancellationToken) =>
        _sender.Send(new GetProductByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();

    /// <summary>
    /// Add stock to a product.
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
        _sender.Send(new AddStockCommand(id, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();
}

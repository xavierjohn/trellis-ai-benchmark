namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Products;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>Products controller.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="ProductsController"/> class.</summary>
    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>Create a new product.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var commandResult = ToCreateProductCommand(request);
        if (!commandResult.TryGetValue(out var command, out var error))
            return error.ToHttpResponse().AsActionResult<ProductResponse>();

        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                ProductResponse.From,
                opts => opts
                    .CreatedAtRoute("Products_GetById", product => product.Id.Value)
                    .WithVersionedRoute())
            .AsActionResultAsync<ProductResponse>();
    }

    /// <summary>Get product by ID.</summary>
    [HttpGet("{id:guid}", Name = "Products_GetById")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetById(ProductId id) => NotFound();

    /// <summary>Add stock to a product.</summary>
    [HttpPost("{id:guid}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductResponse>> AddStock(
        ProductId id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken) =>
        await _sender.Send(new AddStockCommand(id, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();

    private static Result<CreateProductCommand> ToCreateProductCommand(CreateProductRequest request) =>
        ProductName.TryCreate(request.ProductName, nameof(request.ProductName))
            .Combine(Sku.TryCreate(request.Sku, nameof(request.Sku)))
            .Combine(UnitPrice.TryCreate(request.UnitPrice, nameof(request.UnitPrice)))
            .Map(values =>
            {
                var (productName, sku, unitPrice) = values;
                return new CreateProductCommand(productName, sku, unitPrice);
            });
}

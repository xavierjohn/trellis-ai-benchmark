namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;

/// <summary>Product endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Constructor.</summary>
    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>Create a product.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken) =>
        Result.Combine(
                ProductName.TryCreate(request.ProductName, "productName"),
                Sku.TryCreate(request.Sku, "sku"),
                UnitPrice.TryCreate(request.UnitPrice, "unitPrice"))
            .Map(values => new CreateProductCommand(values.Item1, values.Item2, values.Item3))
            .BindAsync(command => _sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(
                ProductResponse.From,
                options => options.Created(product => $"/api/products/{(Guid)product.Id}?api-version=2026-11-12"))
            .AsActionResultAsync<ProductResponse>();

    /// <summary>Add stock to a product.</summary>
    [HttpPost("{id:guid}/stock-additions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<ProductResponse>> AddStock(ProductId id, AddStockRequest request, CancellationToken cancellationToken) =>
        StockAdjustmentQuantity.TryCreate(request.Quantity, "quantity")
            .Map(quantity => new AddStockCommand(id, quantity))
            .BindAsync(command => _sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(ProductResponse.From)
            .AsActionResultAsync<ProductResponse>();
}

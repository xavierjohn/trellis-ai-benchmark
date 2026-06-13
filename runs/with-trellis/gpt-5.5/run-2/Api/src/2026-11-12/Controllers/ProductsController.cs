namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Domain;
using Trellis.Authorization;
using IResult = Microsoft.AspNetCore.Http.IResult;

[Route("api/products")]
public sealed class ProductsController : ApiControllerBase
{
    private readonly OrderManagementService _service;
    private readonly IActorProvider _actorProvider;

    public ProductsController(OrderManagementService service, IActorProvider actorProvider)
    {
        _service = service;
        _actorProvider = actorProvider;
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.ProductsCreate, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);

        var name = ProductName.TryCreate(request.ProductName, "productName");
        if (name.Error is not null) return ToProblem(name.Error);
        var sku = Sku.TryCreate(request.Sku, "sku");
        if (sku.Error is not null) return ToProblem(sku.Error);
        var price = UnitPrice.TryCreate(request.UnitPrice, "unitPrice");
        if (price.Error is not null) return ToProblem(price.Error);

        name.TryGetValue(out var n);
        sku.TryGetValue(out var s);
        price.TryGetValue(out var p);
        var result = await _service.CreateProductAsync(n!, s!, p!, cancellationToken);
        return ToHttp(result, ProductResponse.From, StatusCodes.Status201Created, result.TryGetValue(out var product) ? $"/api/products/{(Guid)product.Id}?api-version=2026-11-12" : null);
    }

    [HttpPost("{id:guid}/stock-additions")]
    [Consumes("application/json")]
    public async Task<IResult> AddStock(Guid id, [FromBody] AddStockRequest request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.ProductsManageStock, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);

        var productId = ProductId.TryCreate(id, "id");
        if (productId.Error is not null) return ToProblem(productId.Error);
        var quantity = OrderQuantity.TryCreate(request.Quantity, "quantity");
        if (quantity.Error is not null) return ToProblem(quantity.Error);

        productId.TryGetValue(out var pid);
        quantity.TryGetValue(out var qty);
        var result = await _service.AddStockAsync(pid!, qty!, cancellationToken);
        return ToHttp(result, ProductResponse.From);
    }
}

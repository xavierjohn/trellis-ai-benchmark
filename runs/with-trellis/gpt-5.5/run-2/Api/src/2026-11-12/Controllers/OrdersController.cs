namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Domain;
using Trellis.Authorization;
using IResult = Microsoft.AspNetCore.Http.IResult;

[Route("api/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly OrderManagementService _service;
    private readonly IActorProvider _actorProvider;

    public OrdersController(OrderManagementService service, IActorProvider actorProvider)
    {
        _service = service;
        _actorProvider = actorProvider;
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersCreate, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        actor.TryGetValue(out var currentActor);

        var customerId = CustomerId.TryCreate(request.CustomerId, "customerId");
        if (customerId.Error is not null) return ToProblem(customerId.Error);
        var items = ParseLineItems(request.LineItems);
        if (items.Error is not null) return ToProblem(items.Error);

        customerId.TryGetValue(out var cid);
        items.TryGetValue(out var parsedItems);
        var result = await _service.CreateOrderAsync(cid!, parsedItems!, currentActor!.Id.Value, cancellationToken);
        return ToHttp(result, OrderResponse.From, StatusCodes.Status201Created, result.TryGetValue(out var order) ? $"/api/orders/{(Guid)order.Id}?api-version=2026-11-12" : null);
    }

    [HttpGet("overdue")]
    public async Task<IResult> Overdue(CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersReadAll, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        var result = await _service.ListOverdueOrdersAsync(cancellationToken);
        return ToHttp(result, orders => orders.Select(OrderResponse.From).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersRead, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        var orderId = OrderId.TryCreate(id, "id");
        if (orderId.Error is not null) return ToProblem(orderId.Error);
        orderId.TryGetValue(out var oid);
        var result = await _service.GetOrderAsync(oid!, cancellationToken);
        return ToHttp(result, OrderResponse.From);
    }

    [HttpPost("{id:guid}/line-items")]
    [Consumes("application/json")]
    public async Task<IResult> AddLineItem(Guid id, [FromBody] AddLineItemRequest request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersCreate, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        var ids = ParseOrderAndProductIds(id, request.ProductId);
        if (ids.Error is not null) return ToProblem(ids.Error);
        var quantity = OrderQuantity.TryCreate(request.Quantity, "quantity");
        if (quantity.Error is not null) return ToProblem(quantity.Error);
        ids.TryGetValue(out var parsedIds);
        quantity.TryGetValue(out var qty);
        var result = await _service.AddLineItemAsync(parsedIds.OrderId, parsedIds.ProductId, qty!, cancellationToken);
        return ToHttp(result, OrderResponse.From);
    }

    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    public async Task<IResult> RemoveLineItem(Guid id, Guid lineItemId, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersCreate, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        var orderId = OrderId.TryCreate(id, "id");
        if (orderId.Error is not null) return ToProblem(orderId.Error);
        var itemId = LineItemId.TryCreate(lineItemId, "lineItemId");
        if (itemId.Error is not null) return ToProblem(itemId.Error);
        orderId.TryGetValue(out var oid);
        itemId.TryGetValue(out var lid);
        var result = await _service.RemoveLineItemAsync(oid!, lid!, cancellationToken);
        return ToHttp(result, OrderResponse.From);
    }

    [HttpPost("{id:guid}/submission")]
    public Task<IResult> Submit(Guid id, CancellationToken cancellationToken) =>
        StateChange(id, Permissions.OrdersSubmit, oid => _service.SubmitAsync(oid, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/approval")]
    public Task<IResult> Approve(Guid id, CancellationToken cancellationToken) =>
        StateChange(id, Permissions.OrdersApprove, oid => _service.ApproveAsync(oid, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/shipment")]
    public Task<IResult> Ship(Guid id, CancellationToken cancellationToken) =>
        StateChange(id, Permissions.OrdersShip, oid => _service.ShipAsync(oid, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/delivery")]
    public Task<IResult> Deliver(Guid id, CancellationToken cancellationToken) =>
        StateChange(id, Permissions.OrdersDeliver, oid => _service.DeliverAsync(oid, cancellationToken), cancellationToken);

    [HttpPost("{id:guid}/cancellation")]
    public async Task<IResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersCancel, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        actor.TryGetValue(out var currentActor);
        var orderId = OrderId.TryCreate(id, "id");
        if (orderId.Error is not null) return ToProblem(orderId.Error);
        orderId.TryGetValue(out var oid);

        var existing = await _service.GetOrderAsync(oid!, cancellationToken);
        if (existing.Error is not null) return ToProblem(existing.Error);
        existing.TryGetValue(out var order);
        if (order!.CreatedByActorId != currentActor!.Id.Value && !currentActor.HasPermission(Permissions.OrdersReadAll))
            return ToProblem(new Error.Forbidden("orders.cancel.owner", ResourceRef.For<Order>(oid!)) { Detail = "Only the order creator or an admin can cancel this order." });

        var result = await _service.CancelAsync(oid!, cancellationToken);
        return ToHttp(result, OrderResponse.From);
    }

    private async Task<IResult> StateChange(Guid id, string permission, Func<OrderId, Task<Result<Order>>> action, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, permission, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);
        var orderId = OrderId.TryCreate(id, "id");
        if (orderId.Error is not null) return ToProblem(orderId.Error);
        orderId.TryGetValue(out var oid);
        var result = await action(oid!);
        return ToHttp(result, OrderResponse.From);
    }

    private static Result<IReadOnlyList<(ProductId ProductId, OrderQuantity Quantity)>> ParseLineItems(IReadOnlyList<CreateOrderLineItemRequest>? items)
    {
        if (items is null || items.Count == 0)
            return Result.Fail<IReadOnlyList<(ProductId, OrderQuantity)>>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        var parsed = new List<(ProductId, OrderQuantity)>();
        foreach (var item in items)
        {
            var productId = ProductId.TryCreate(item.ProductId, "lineItems.productId");
            if (productId.Error is not null) return Result.Fail<IReadOnlyList<(ProductId, OrderQuantity)>>(productId.Error);
            var quantity = OrderQuantity.TryCreate(item.Quantity, "lineItems.quantity");
            if (quantity.Error is not null) return Result.Fail<IReadOnlyList<(ProductId, OrderQuantity)>>(quantity.Error);
            productId.TryGetValue(out var pid);
            quantity.TryGetValue(out var qty);
            parsed.Add((pid!, qty!));
        }

        return parsed.Select(i => i.Item1).Distinct().Count() == parsed.Count
            ? Result.Ok<IReadOnlyList<(ProductId, OrderQuantity)>>(parsed)
            : Result.Fail<IReadOnlyList<(ProductId, OrderQuantity)>>(Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear in multiple line items."));
    }

    private static Result<(OrderId OrderId, ProductId ProductId)> ParseOrderAndProductIds(Guid orderId, Guid productId)
    {
        var parsedOrderId = OrderId.TryCreate(orderId, "id");
        if (parsedOrderId.Error is not null) return Result.Fail<(OrderId, ProductId)>(parsedOrderId.Error);
        var parsedProductId = ProductId.TryCreate(productId, "productId");
        if (parsedProductId.Error is not null) return Result.Fail<(OrderId, ProductId)>(parsedProductId.Error);
        parsedOrderId.TryGetValue(out var oid);
        parsedProductId.TryGetValue(out var pid);
        return Result.Ok((oid!, pid!));
    }
}

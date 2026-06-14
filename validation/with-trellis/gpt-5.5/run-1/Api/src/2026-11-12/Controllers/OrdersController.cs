namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>Order endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>Create a draft order.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken) =>
        sender.Send(
                new CreateDraftOrderCommand(
                    request.CustomerId,
                    request.LineItems.Select(item => new RequestedLineItem(item.ProductId, item.Quantity)).ToArray()),
                cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                opts => opts
                    .CreatedAtRoute("Orders_GetById", order => new Microsoft.AspNetCore.Routing.RouteValueDictionary { ["id"] = (Guid)order.Id })
                    .WithVersionedRoute())
            .AsActionResultAsync<OrderResponse>();

    /// <summary>List overdue orders.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> Overdue(CancellationToken cancellationToken) =>
        sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    /// <summary>Get an order by id.</summary>
    [HttpGet("{id}", Name = "Orders_GetById")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<OrderResponse>> GetById(OrderId id, CancellationToken cancellationToken) =>
        sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Add a line item.</summary>
    [HttpPost("{id}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> AddLineItem(OrderId id, [FromBody] AddLineItemRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddLineItemCommand(id, request.ProductId, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Remove a line item.</summary>
    [HttpDelete("{id}/line-items/{lineItemId}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> RemoveLineItem(OrderId id, LineItemId lineItemId, CancellationToken cancellationToken) =>
        sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Submit an order.</summary>
    [HttpPost("{id}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Submit(OrderId id, CancellationToken cancellationToken) =>
        sender.Send(new SubmitOrderCommand(id), cancellationToken).ToHttpResponseAsync(OrderResponse.From).AsActionResultAsync<OrderResponse>();

    /// <summary>Approve an order.</summary>
    [HttpPost("{id}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Approve(OrderId id, CancellationToken cancellationToken) =>
        sender.Send(new ApproveOrderCommand(id), cancellationToken).ToHttpResponseAsync(OrderResponse.From).AsActionResultAsync<OrderResponse>();

    /// <summary>Ship an order.</summary>
    [HttpPost("{id}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Ship(OrderId id, CancellationToken cancellationToken) =>
        sender.Send(new ShipOrderCommand(id), cancellationToken).ToHttpResponseAsync(OrderResponse.From).AsActionResultAsync<OrderResponse>();

    /// <summary>Deliver an order.</summary>
    [HttpPost("{id}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Deliver(OrderId id, CancellationToken cancellationToken) =>
        sender.Send(new DeliverOrderCommand(id), cancellationToken).ToHttpResponseAsync(OrderResponse.From).AsActionResultAsync<OrderResponse>();

    /// <summary>Cancel an order.</summary>
    [HttpPost("{id}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Cancel(OrderId id, CancellationToken cancellationToken) =>
        sender.Send(new CancelOrderCommand(id), cancellationToken).ToHttpResponseAsync(OrderResponse.From).AsActionResultAsync<OrderResponse>();
}

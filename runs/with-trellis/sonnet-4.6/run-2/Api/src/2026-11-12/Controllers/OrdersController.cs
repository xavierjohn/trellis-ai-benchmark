namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>
/// Orders controller.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Constructor.</summary>
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create a new draft order.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Create(
        [FromBody] CreateDraftOrderRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(
            new CreateDraftOrderCommand(
                request.CustomerId,
                request.LineItems.Select(li => (li.ProductId, li.Quantity)).ToList()),
            cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                opts => opts
                    .CreatedAtRoute("Orders_GetById", o => new Microsoft.AspNetCore.Routing.RouteValueDictionary
                    {
                        ["id"] = (Guid)o.Id
                    })
                    .WithVersionedRoute())
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Get all overdue orders.
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> GetOverdue(CancellationToken cancellationToken) =>
        _sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToList().AsReadOnly() as IReadOnlyList<OrderResponse>)
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    /// <summary>
    /// Get an order by ID.
    /// </summary>
    [HttpGet("{id}", Name = "Orders_GetById")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<OrderResponse>> GetById(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Add a line item to a draft order.
    /// </summary>
    [HttpPost("{id}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> AddLineItem(
        OrderId id,
        [FromBody] AddLineItemRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(new AddLineItemCommand(id, request.ProductId, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Remove a line item from a draft order.
    /// </summary>
    [HttpDelete("{id}/line-items/{lineItemId}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> RemoveLineItem(
        OrderId id,
        LineItemId lineItemId,
        CancellationToken cancellationToken) =>
        _sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Submit an order for approval.
    /// </summary>
    [HttpPost("{id}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Submit(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new SubmitOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Approve a submitted order.
    /// </summary>
    [HttpPost("{id}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Approve(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new ApproveOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Ship an approved order.
    /// </summary>
    [HttpPost("{id}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Ship(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new ShipOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Deliver an order.
    /// </summary>
    [HttpPost("{id}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Deliver(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new DeliverOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>
    /// Cancel an order.
    /// </summary>
    [HttpPost("{id}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Cancel(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new CancelOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();
}

namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;

/// <summary>Order endpoints, including lifecycle state transitions.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Creates a draft order.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var lines = request.Lines is null
            ? []
            : request.Lines.Select(l => new OrderLineInput(l.ProductId, l.Quantity)).ToList();
        return _sender.Send(new CreateDraftOrderCommand(request.CustomerId, lines), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                opts => opts.Created(o => $"/api/orders/{o.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<OrderResponse>();
    }

    /// <summary>Gets an order by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<OrderResponse>> GetById(
        OrderId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Lists overdue orders (Submitted for more than 7 days without approval).</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> GetOverdue(
        CancellationToken cancellationToken) =>
        _sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .ToHttpResponseAsync(orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    /// <summary>Adds a line item to a draft order.</summary>
    [HttpPost("{id}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>Removes a line item from a draft order.</summary>
    [HttpDelete("{id}/line-items/{lineItemId}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> RemoveLineItem(
        OrderId id,
        LineItemId lineItemId,
        CancellationToken cancellationToken) =>
        _sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Submits a draft order, reserving stock.</summary>
    [HttpPost("{id}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Submit(
        OrderId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new SubmitOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Approves a submitted order.</summary>
    [HttpPost("{id}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Approve(
        OrderId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new ApproveOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Ships an approved order.</summary>
    [HttpPost("{id}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Ship(
        OrderId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new ShipOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Marks a shipped order as delivered.</summary>
    [HttpPost("{id}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Deliver(
        OrderId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new DeliverOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Cancels an order (subject to the ownership check).</summary>
    [HttpPost("{id}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Cancel(
        OrderId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new CancelOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();
}

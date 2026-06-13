namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>Order endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Creates a draft order.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateDraftOrderRequest request, CancellationToken cancellationToken)
    {
        var commandResult = CreateDraftOrderCommand.TryCreate(
            request.CustomerId,
            request.LineItems.Select(lineItem => new CreateDraftOrderInput(lineItem.ProductId, lineItem.Quantity)).ToList());
        if (!commandResult.TryGetValue(out var command))
            return commandResult.Error!.ToHttpResponse().AsActionResult<OrderResponse>();

        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .CreatedAtRoute("Orders_GetById", order => new RouteValueDictionary { ["id"] = (Guid)order.Id })
                    .WithVersionedRoute()
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();
    }

    /// <summary>Adds a line item to a draft order.</summary>
    [HttpPost("{id}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> AddLineItem(OrderId id, [FromBody] AddLineItemRequest request, CancellationToken cancellationToken) =>
        _sender.Send(new AddLineItemCommand(id, request.ProductId, request.Quantity), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Removes a line item from a draft order.</summary>
    [HttpDelete("{id}/line-items/{lineItemId}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> RemoveLineItem(OrderId id, LineItemId lineItemId, CancellationToken cancellationToken) =>
        _sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Gets an order by identifier.</summary>
    [HttpGet("{id}", Name = "Orders_GetById")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public ValueTask<ActionResult<OrderResponse>> GetById(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified)
                    .EvaluatePreconditions())
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Submits a draft order.</summary>
    [HttpPost("{id}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Submit(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new SubmitOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Approves a submitted order.</summary>
    [HttpPost("{id}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Approve(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new ApproveOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Ships an approved order.</summary>
    [HttpPost("{id}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Ship(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new ShipOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Delivers a shipped order.</summary>
    [HttpPost("{id}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Deliver(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new DeliverOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Cancels an order.</summary>
    [HttpPost("{id}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<OrderResponse>> Cancel(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new CancelOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .WithETag(order => EntityTagValue.Strong(order.ETag))
                    .WithLastModified(order => order.LastModified))
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Lists overdue submitted orders.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> GetOverdue(CancellationToken cancellationToken) =>
        _sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .ToHttpResponseAsync(orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();
}

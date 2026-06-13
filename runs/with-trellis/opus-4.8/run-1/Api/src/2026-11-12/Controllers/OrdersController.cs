namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;

/// <summary>Order endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    private const string ApiVersion = "2026-11-12";

    /// <summary>Creates a draft order for a customer.</summary>
    /// <param name="request">The order to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateDraftOrderCommand(request.CustomerId, request.Lines), cancellationToken);

        return result
            .ToHttpResponse(
                body: OrderResponse.From,
                configure: opts => opts.Created(o => $"/api/orders/{o.Id.Value}?api-version={ApiVersion}"))
            .AsActionResult<OrderResponse>();
    }

    /// <summary>Lists overdue orders (Submitted for more than 7 days).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("overdue")]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> ListOverdue(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListOverdueOrdersQuery(), cancellationToken);
        return result
            .ToHttpResponse(body: orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResult<IReadOnlyList<OrderResponse>>();
    }

    /// <summary>Gets an order by identifier.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}", Name = "orders.get")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(OrderId id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Adds a line item to a draft order.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="request">Product and quantity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> AddLineItem(
        OrderId id,
        [FromBody] AddLineItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddLineItemCommand(id, request.ProductId, request.Quantity), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Removes a line item from a draft order.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="lineItemId">Line item identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id}/line-items/{lineItemId}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> RemoveLineItem(
        OrderId id,
        LineItemId lineItemId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Submits a draft order, reserving stock.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/submission")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Submit(OrderId id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitOrderCommand(id), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Approves a submitted order.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/approval")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Approve(OrderId id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveOrderCommand(id), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Ships an approved order.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/shipment")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Ship(OrderId id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ShipOrderCommand(id), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Marks a shipped order as delivered.</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/delivery")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Deliver(OrderId id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeliverOrderCommand(id), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }

    /// <summary>Cancels an order (owner or administrator only).</summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id}/cancellation")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Cancel(OrderId id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(id), cancellationToken);
        return result.ToHttpResponse(body: OrderResponse.From).AsActionResult<OrderResponse>();
    }
}

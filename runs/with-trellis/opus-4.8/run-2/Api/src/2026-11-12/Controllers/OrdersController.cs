namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>Order endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>Creates a new draft order.</summary>
    /// <param name="request">The order to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created order.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var lines = (request.Lines ?? Array.Empty<CreateOrderLineDto>())
            .TraverseAll(line => Result.Combine(
                    ProductId.TryCreate(line.ProductId, "productId"),
                    Quantity.TryCreate(line.Quantity, "quantity"))
                .Map(t => new OrderLineRequest(t.Item1, t.Item2)));

        var command = Result.Combine(CustomerId.TryCreate(request.CustomerId, "customerId"), lines)
            .Map(t => new CreateDraftOrderCommand(t.Item1, t.Item2));

        return await command
            .BindAsync(c => sender.Send(c, cancellationToken))
            .ToHttpResponseAsync(
                OrderResponse.From,
                opts => opts.CreatedAtRoute("orders.get", o => o.Id.Value).WithVersionedRoute())
            .AsActionResultAsync<OrderResponse>();
    }

    /// <summary>Gets an order by id.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order.</returns>
    [HttpGet("{id:guid}", Name = "orders.get")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Get(OrderId id, CancellationToken cancellationToken) =>
        await sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Lists overdue orders (submitted more than 7 days ago without approval).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The overdue orders.</returns>
    [HttpGet("overdue")]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> Overdue(CancellationToken cancellationToken) =>
        await sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    /// <summary>Adds a line item to a draft order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="request">The line item to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    [HttpPost("{id:guid}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> AddLineItem(
        OrderId id,
        [FromBody] AddLineItemRequest request,
        CancellationToken cancellationToken) =>
        await Result.Combine(
                ProductId.TryCreate(request.ProductId, "productId"),
                Quantity.TryCreate(request.Quantity, "quantity"))
            .Map(t => new AddLineItemCommand(id, t.Item1, t.Item2))
            .BindAsync(c => sender.Send(c, cancellationToken))
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Removes a line item from a draft order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="lineItemId">Line item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> RemoveLineItem(
        OrderId id,
        LineItemId lineItemId,
        CancellationToken cancellationToken) =>
        await sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Submits a draft order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The submitted order.</returns>
    [HttpPost("{id:guid}/submission")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Submit(OrderId id, CancellationToken cancellationToken) =>
        await sender.Send(new SubmitOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Approves a submitted order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approved order.</returns>
    [HttpPost("{id:guid}/approval")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Approve(OrderId id, CancellationToken cancellationToken) =>
        await sender.Send(new ApproveOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Ships an approved order.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shipped order.</returns>
    [HttpPost("{id:guid}/shipment")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Ship(OrderId id, CancellationToken cancellationToken) =>
        await sender.Send(new ShipOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Marks a shipped order as delivered.</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivered order.</returns>
    [HttpPost("{id:guid}/delivery")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Deliver(OrderId id, CancellationToken cancellationToken) =>
        await sender.Send(new DeliverOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Cancels an order (subject to ownership check).</summary>
    /// <param name="id">Order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled order.</returns>
    [HttpPost("{id:guid}/cancellation")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<OrderResponse>> Cancel(OrderId id, CancellationToken cancellationToken) =>
        await sender.Send(new CancelOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();
}

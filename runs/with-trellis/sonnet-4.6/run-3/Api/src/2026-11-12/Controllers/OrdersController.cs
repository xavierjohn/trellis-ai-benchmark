namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;

/// <summary>Orders controller.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="OrdersController"/> class.</summary>
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Create a new draft order.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateDraftOrderRequest request,
        CancellationToken cancellationToken)
    {
        var commandResult = ToCreateDraftOrderCommand(request);
        if (!commandResult.TryGetValue(out var command, out var error))
            return error.ToHttpResponse().AsActionResult<OrderResponse>();

        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                OrderResponse.From,
                opts => opts
                    .CreatedAtRoute("Orders_GetById", order => order.Id.Value)
                    .WithVersionedRoute())
            .AsActionResultAsync<OrderResponse>();
    }

    /// <summary>List overdue orders.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetOverdue(CancellationToken cancellationToken) =>
        await _sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .ToHttpResponseAsync(orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    /// <summary>Get an order by ID.</summary>
    [HttpGet("{id:guid}", Name = "Orders_GetById")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(
        OrderId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Add a line item to a draft order.</summary>
    [HttpPost("{id:guid}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> AddLineItem(
        OrderId id,
        [FromBody] AddLineItemRequest request,
        CancellationToken cancellationToken)
    {
        var commandResult = ProductId.TryCreate(request.ProductId, nameof(request.ProductId))
            .Map(productId => new AddLineItemCommand(id, productId, request.Quantity));

        if (!commandResult.TryGetValue(out var command, out var error))
            return error.ToHttpResponse().AsActionResult<OrderResponse>();

        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();
    }

    /// <summary>Remove a line item from a draft order.</summary>
    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> RemoveLineItem(
        OrderId id,
        LineItemId lineItemId,
        CancellationToken cancellationToken) =>
        await _sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Submit a draft order.</summary>
    [HttpPost("{id:guid}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Submit(
        OrderId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new SubmitOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Approve a submitted order.</summary>
    [HttpPost("{id:guid}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Approve(
        OrderId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new ApproveOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Ship an approved order.</summary>
    [HttpPost("{id:guid}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Ship(
        OrderId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new ShipOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Deliver a shipped order.</summary>
    [HttpPost("{id:guid}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Deliver(
        OrderId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new DeliverOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Cancel an order.</summary>
    [HttpPost("{id:guid}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Cancel(
        OrderId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new CancelOrderCommand(id), cancellationToken)
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    private static Result<CreateDraftOrderCommand> ToCreateDraftOrderCommand(CreateDraftOrderRequest request) =>
        CustomerId.TryCreate(request.CustomerId, nameof(request.CustomerId))
            .Combine(ToOrderLineItemInputs(request.LineItems))
            .Bind(values =>
            {
                var (customerId, lineItems) = values;
                return CreateDraftOrderCommand.TryCreate(customerId, lineItems);
            });

    private static Result<IReadOnlyList<OrderLineItemInput>> ToOrderLineItemInputs(IReadOnlyList<LineItemRequest> lineItems) =>
        lineItems
            .Select((lineItem, index) => new { lineItem, index })
            .TraverseAll(item =>
                ProductId.TryCreate(item.lineItem.ProductId, $"lineItems[{item.index}].productId")
                    .Map(productId => new OrderLineItemInput(productId, item.lineItem.Quantity)))
            .Map(items => (IReadOnlyList<OrderLineItemInput>)items.ToList());
}

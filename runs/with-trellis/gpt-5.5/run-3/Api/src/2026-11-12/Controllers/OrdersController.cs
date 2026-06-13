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
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Constructor.</summary>
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Create a draft order.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken) =>
        ToCreateCommand(request)
            .BindAsync(command => _sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(
                OrderResponse.From,
                options => options
                    .CreatedAtRoute("Orders_GetById", order => (Guid)order.Id)
                    .WithVersionedRoute())
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Add a line item to a draft order.</summary>
    [HttpPost("{id:guid}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> AddLineItem(OrderId id, AddLineItemRequest request, CancellationToken cancellationToken) =>
        Result.Combine(
                ProductId.TryCreate(request.ProductId, "productId"),
                LineItemQuantity.TryCreate(request.Quantity, "quantity"))
            .Map(values => new AddLineItemCommand(id, values.Item1, values.Item2))
            .BindAsync(command => _sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Remove a line item from a draft order.</summary>
    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> RemoveLineItem(OrderId id, LineItemId lineItemId, CancellationToken cancellationToken) =>
        _sender.Send(new RemoveLineItemCommand(id, lineItemId), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    /// <summary>Submit a draft order.</summary>
    [HttpPost("{id:guid}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Submit(OrderId id, CancellationToken cancellationToken) =>
        Send(_sender.Send(new SubmitOrderCommand(id), cancellationToken).AsTask());

    /// <summary>Approve a submitted order.</summary>
    [HttpPost("{id:guid}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Approve(OrderId id, CancellationToken cancellationToken) =>
        Send(_sender.Send(new ApproveOrderCommand(id), cancellationToken).AsTask());

    /// <summary>Ship an approved order.</summary>
    [HttpPost("{id:guid}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Ship(OrderId id, CancellationToken cancellationToken) =>
        Send(_sender.Send(new ShipOrderCommand(id), cancellationToken).AsTask());

    /// <summary>Mark a shipped order as delivered.</summary>
    [HttpPost("{id:guid}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Deliver(OrderId id, CancellationToken cancellationToken) =>
        Send(_sender.Send(new DeliverOrderCommand(id), cancellationToken).AsTask());

    /// <summary>Cancel an order.</summary>
    [HttpPost("{id:guid}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Cancel(OrderId id, CancellationToken cancellationToken) =>
        Send(_sender.Send(new CancelOrderCommand(id), cancellationToken).AsTask());

    /// <summary>List overdue orders.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<ActionResult<IReadOnlyList<OrderResponse>>> ListOverdue(CancellationToken cancellationToken) =>
        _sender.Send(new ListOverdueOrdersQuery(), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    /// <summary>Get an order by ID.</summary>
    [HttpGet("{id:guid}", Name = "Orders_GetById")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> GetById(OrderId id, CancellationToken cancellationToken) =>
        _sender.Send(new GetOrderByIdQuery(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    private static Task<ActionResult<OrderResponse>> Send(Task<Result<Order>> result) =>
        result
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    private static Result<CreateDraftOrderCommand> ToCreateCommand(CreateOrderRequest request)
    {
        var customerId = CustomerId.TryCreate(request.CustomerId, "customerId");
        var lines = request.LineItems ?? [];
        if (lines.Count == 0)
            return Result.Fail<CreateDraftOrderCommand>(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));

        var parsedLines = new List<DraftOrderLine>(lines.Count);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var parsedLine = Result.Combine(
                    ProductId.TryCreate(line.ProductId, $"lineItems/{index}/productId"),
                    LineItemQuantity.TryCreate(line.Quantity, $"lineItems/{index}/quantity"))
                .Map(values => new DraftOrderLine(values.Item1, values.Item2));
            if (parsedLine.IsFailure)
                return Result.Fail<CreateDraftOrderCommand>(parsedLine.Error!);
            parsedLine.TryGetValue(out var draftLine);
            parsedLines.Add(draftLine!);
        }

        return customerId.Map(id => new CreateDraftOrderCommand(id, parsedLines));
    }
}

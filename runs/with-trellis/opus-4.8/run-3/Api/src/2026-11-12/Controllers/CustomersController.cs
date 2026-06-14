namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Primitives;

/// <summary>Customer endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public CustomersController(ISender sender) => _sender = sender;

    /// <summary>Creates a new customer.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var phone = request.Phone is null ? Maybe<PhoneNumber>.None : Maybe.From(request.Phone);
        var command = new CreateCustomerCommand(
            request.FirstName, request.LastName, request.Email, phone, request.ShippingAddress);
        return _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                CustomerResponse.From,
                opts => opts.Created(c => $"/api/customers/{c.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<CustomerResponse>();
    }

    /// <summary>Lists the orders belonging to a customer.</summary>
    [HttpGet("{id}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(
        CustomerId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();
}

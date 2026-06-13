namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;

/// <summary>Customer endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/customers")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    private const string ApiVersion = "2026-11-12";

    /// <summary>Creates a new customer.</summary>
    /// <param name="request">The customer to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await request.ShippingAddress.ToShippingAddress()
            .Map(address => new CreateCustomerCommand(
                request.FirstName, request.LastName, request.Email, request.Phone, address))
            .BindAsync(command => sender.Send(command, cancellationToken).AsTask());

        return result
            .ToHttpResponse(
                body: CustomerResponse.From,
                configure: opts => opts.Created(c => $"/api/customers/{c.Id.Value}?api-version={ApiVersion}"))
            .AsActionResult<CustomerResponse>();
    }

    /// <summary>Lists all orders belonging to a customer.</summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}/orders")]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(
        CustomerId id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken);
        return result
            .ToHttpResponse(body: orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResult<IReadOnlyList<OrderResponse>>();
    }
}

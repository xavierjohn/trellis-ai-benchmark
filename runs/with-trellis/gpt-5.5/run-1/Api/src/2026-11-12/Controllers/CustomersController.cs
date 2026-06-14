namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;

/// <summary>
/// Customer endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a customer.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken) =>
        sender.Send(new CreateCustomerCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.ShippingAddress.ToDomain()), cancellationToken)
            .ToHttpResponseAsync(
                CustomerResponse.From,
                options => options.Created(customer => $"/api/customers/{customer.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<CustomerResponse>();

    /// <summary>
    /// Lists orders for a customer.
    /// </summary>
    [HttpGet("{id}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(
        CustomerId id,
        CancellationToken cancellationToken) =>
        sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();
}

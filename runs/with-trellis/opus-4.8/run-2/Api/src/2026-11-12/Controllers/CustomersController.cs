namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Primitives;

/// <summary>Customer endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/customers")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    /// <summary>Creates a new customer.</summary>
    /// <param name="request">The customer to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created customer.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var address = request.ShippingAddress;

        var command = Result.Combine(
                FirstName.TryCreate(request.FirstName, "firstName"),
                LastName.TryCreate(request.LastName, "lastName"),
                EmailAddress.TryCreate(request.Email, "email"),
                BuildPhone(request.PhoneNumber),
                ShippingAddress.TryCreate(
                    address?.Street, address?.City, address?.State, address?.PostalCode, address?.Country, "shippingAddress"))
            .Map(t => new CreateCustomerCommand(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5));

        return await command
            .BindAsync(c => sender.Send(c, cancellationToken))
            .ToHttpResponseAsync(
                CustomerResponse.From,
                opts => opts.Created(c => $"/api/customers/{c.Id.Value}?api-version=2026-11-12"))
            .AsActionResultAsync<CustomerResponse>();
    }

    /// <summary>Lists all orders belonging to a customer.</summary>
    /// <param name="id">Customer id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer's orders.</returns>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(
        CustomerId id,
        CancellationToken cancellationToken) =>
        await sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    private static Result<Maybe<PhoneNumber>> BuildPhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone)
            ? Result.Ok(Maybe<PhoneNumber>.None)
            : PhoneNumber.TryCreate(phone, "phoneNumber").Map(Maybe.From);
}

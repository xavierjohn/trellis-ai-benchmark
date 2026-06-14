namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;
using Trellis.Primitives;

/// <summary>Customer endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/customers")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    /// <summary>Create a customer.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var address = request.ShippingAddress is null
            ? Result.Fail<ShippingAddress>(Error.InvalidInput.ForField("shippingAddress", "required", "Shipping address is required."))
            : ShippingAddress.TryCreate(request.ShippingAddress.Street, request.ShippingAddress.City, request.ShippingAddress.State, request.ShippingAddress.PostalCode, request.ShippingAddress.Country);

        return FirstName.TryCreate(request.FirstName, "firstName")
            .Combine(LastName.TryCreate(request.LastName, "lastName"))
            .Combine(EmailAddress.TryCreate(request.Email, "email"))
            .Combine(Maybe.Optional(request.PhoneNumber, phone => PhoneNumber.TryCreate(phone, "phoneNumber")))
            .Combine(address)
            .Map((first, last, email, phone, shipping) => new CreateCustomerCommand(first, last, email, phone, shipping))
            .BindAsync(command => sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(
                CustomerResponse.From,
                opts => opts
                    .Created(customer => $"/api/customers/{((Guid)customer.Id).ToString()}?api-version=2026-11-12")
                    .WithVersionedRoute())
            .AsActionResultAsync<CustomerResponse>();
    }

    /// <summary>List orders by customer.</summary>
    [HttpGet("{id}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(CustomerId id, CancellationToken cancellationToken) =>
        sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();
}

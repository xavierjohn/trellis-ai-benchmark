namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Asp;
using Trellis.Primitives;

/// <summary>Customer endpoints.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    public CustomersController(ISender sender) => _sender = sender;

    /// <summary>Creates a customer.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var phoneResult = TryCreatePhoneNumber(request.PhoneNumber);
        if (!phoneResult.TryGetValue(out var phoneNumber))
            return phoneResult.Error!.ToHttpResponse().AsActionResult<CustomerResponse>();

        var shippingAddressResult = ShippingAddress.TryCreate(
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country);
        if (!shippingAddressResult.TryGetValue(out var shippingAddress))
            return shippingAddressResult.Error!.ToHttpResponse().AsActionResult<CustomerResponse>();

        return await _sender.Send(
                new CreateCustomerCommand(request.FirstName, request.LastName, request.Email, phoneNumber, shippingAddress),
                cancellationToken)
            .ToHttpResponseAsync(
                CustomerResponse.From,
                options => options
                    .Created(customer => $"/api/customers/{customer.Id.Value}?api-version=2026-11-12")
                    .WithETag(customer => EntityTagValue.Strong(customer.ETag))
                    .WithLastModified(customer => customer.LastModified))
            .AsActionResultAsync<CustomerResponse>();
    }

    /// <summary>Lists orders for a customer.</summary>
    [HttpGet("{id}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> GetOrders(CustomerId id, CancellationToken cancellationToken) =>
        _sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    private static Result<Maybe<PhoneNumber>> TryCreatePhoneNumber(string? rawPhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(rawPhoneNumber))
            return Result.Ok(Maybe<PhoneNumber>.None);

        return PhoneNumber.TryCreate(rawPhoneNumber, "phoneNumber").Map(Maybe.From);
    }
}

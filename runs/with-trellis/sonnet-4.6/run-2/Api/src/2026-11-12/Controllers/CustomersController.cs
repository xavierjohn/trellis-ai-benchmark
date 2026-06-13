namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis;
using Trellis.Asp;
using Trellis.Asp.ApiVersioning;
using Trellis.Primitives;

/// <summary>
/// Customers controller.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Constructor.</summary>
    public CustomersController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create a new customer.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        Result<Maybe<PhoneNumber>> phoneResult = request.PhoneNumber is not null
            ? PhoneNumber.TryCreate(request.PhoneNumber).Map(p => Maybe.From(p))
            : Result.Ok(Maybe<PhoneNumber>.None);

        return phoneResult
            .BindAsync(phone => ShippingAddress.TryCreate(
                request.ShippingAddress?.Street,
                request.ShippingAddress?.City,
                request.ShippingAddress?.State,
                request.ShippingAddress?.PostalCode,
                request.ShippingAddress?.Country)
                .BindAsync(address => _sender.Send(
                    new CreateCustomerCommand(request.FirstName, request.LastName, request.Email, phone, address),
                    cancellationToken).AsTask()))
            .ToHttpResponseAsync(
                CustomerResponse.From,
                opts => opts
                    .CreatedAtRoute("Customers_GetById", c => new Microsoft.AspNetCore.Routing.RouteValueDictionary
                    {
                        ["id"] = (Guid)c.Id
                    })
                    .WithVersionedRoute())
            .AsActionResultAsync<CustomerResponse>();
    }

    /// <summary>
    /// Get a customer by ID.
    /// </summary>
    [HttpGet("{id}", Name = "Customers_GetById")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<CustomerResponse>> GetById(CustomerId id, CancellationToken cancellationToken) =>
        _sender.Send(new GetCustomerByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(CustomerResponse.From)
            .AsActionResultAsync<CustomerResponse>();

    /// <summary>
    /// List all orders for a customer.
    /// </summary>
    [HttpGet("{id}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<IReadOnlyList<OrderResponse>>> GetOrders(CustomerId id, CancellationToken cancellationToken) =>
        _sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToList().AsReadOnly() as IReadOnlyList<OrderResponse>)
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();
}

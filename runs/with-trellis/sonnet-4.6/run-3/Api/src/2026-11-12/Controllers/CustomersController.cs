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

/// <summary>Customers controller.</summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="CustomersController"/> class.</summary>
    public CustomersController(ISender sender) => _sender = sender;

    /// <summary>Create a new customer.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var commandResult = ToCreateCustomerCommand(request);
        if (!commandResult.TryGetValue(out var command, out var error))
            return error.ToHttpResponse().AsActionResult<CustomerResponse>();

        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                CustomerResponse.From,
                opts => opts
                    .CreatedAtRoute("Customers_GetById", customer => customer.Id.Value)
                    .WithVersionedRoute())
            .AsActionResultAsync<CustomerResponse>();
    }

    /// <summary>Get customer by ID.</summary>
    [HttpGet("{id:guid}", Name = "Customers_GetById")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetById(CustomerId id) => NotFound();

    /// <summary>List orders for a customer.</summary>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetOrders(
        CustomerId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .ToHttpResponseAsync(orders => (IReadOnlyList<OrderResponse>)orders.Select(OrderResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    private static Result<CreateCustomerCommand> ToCreateCustomerCommand(CreateCustomerRequest request) =>
        FirstName.TryCreate(request.FirstName, nameof(request.FirstName))
            .Combine(LastName.TryCreate(request.LastName, nameof(request.LastName)))
            .Combine(Email.TryCreate(request.Email, nameof(request.Email)))
            .Combine(ToPhoneNumber(request.PhoneNumber))
            .Combine(ShippingAddress.TryCreate(
                request.ShippingAddress?.Street,
                request.ShippingAddress?.City,
                request.ShippingAddress?.State,
                request.ShippingAddress?.PostalCode,
                request.ShippingAddress?.Country))
            .Map(values =>
            {
                var (firstName, lastName, email, phoneNumber, shippingAddress) = values;
                return new CreateCustomerCommand(firstName, lastName, email, phoneNumber, shippingAddress);
            });

    private static Result<Maybe<PhoneNumber>> ToPhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber)
            ? Result.Ok(Maybe<PhoneNumber>.None)
            : PhoneNumber.TryCreate(phoneNumber, nameof(CreateCustomerRequest.PhoneNumber))
                .Map(Maybe.From);
}

namespace OrderManagement.Api.v2026_11_12.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.v2026_11_12.Models;
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

    /// <summary>Constructor.</summary>
    public CustomersController(ISender sender) => _sender = sender;

    /// <summary>Create a customer.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken) =>
        ToCommand(request)
            .BindAsync(command => _sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(
                CustomerResponse.From,
                options => options.Created(customer => $"/api/customers/{(Guid)customer.Id}?api-version=2026-11-12"))
            .AsActionResultAsync<CustomerResponse>();

    /// <summary>List orders by customer.</summary>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(CustomerId id, CancellationToken cancellationToken) =>
        _sender.Send(new ListOrdersByCustomerQuery(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(orders => orders.Select(OrderResponse.From).ToArray())
            .AsActionResultAsync<IReadOnlyList<OrderResponse>>();

    private static Result<CreateCustomerCommand> ToCommand(CreateCustomerRequest request)
    {
        var address = request.ShippingAddress;
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? Result.Ok(Maybe<PhoneNumber>.None)
            : PhoneNumber.TryCreate(request.PhoneNumber, "phoneNumber").Map(Maybe.From);

        return Result.Combine(
                FirstName.TryCreate(request.FirstName, "firstName"),
                LastName.TryCreate(request.LastName, "lastName"),
                EmailAddress.TryCreate(request.Email, "email"),
                phone,
                ShippingAddress.TryCreate(address?.Street, address?.City, address?.State, address?.PostalCode, address?.Country))
            .Map(values => new CreateCustomerCommand(values.Item1, values.Item2, values.Item3, values.Item4, values.Item5));
    }
}

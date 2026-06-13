namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Domain;
using Trellis;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Customer endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public sealed class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>Constructor.</summary>
    public CustomersController(AppDbContext db) => _db = db;

    /// <summary>Create a customer.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.CustomersCreate) is { } forbidden)
            return forbidden;

        var address = request.ShippingAddress is null
            ? null
            : new ShippingAddressInput(
                request.ShippingAddress.Street ?? string.Empty,
                request.ShippingAddress.City ?? string.Empty,
                request.ShippingAddress.State ?? string.Empty,
                request.ShippingAddress.PostalCode ?? string.Empty,
                request.ShippingAddress.Country ?? string.Empty);

        var result = Customer.TryCreate(request.FirstName, request.LastName, request.Email, request.PhoneNumber, address);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        if (!result.TryGetValue(out var customer))
            return ApiSupport.Problem(this, result.Error!);

        if (await _db.Customers.AnyAsync(c => c.Email == customer.Email, cancellationToken))
            return ApiSupport.Problem(this, new Error.Conflict(ResourceRef.For("Customer", customer.Email), "duplicate.email") { Detail = "A customer with this email already exists." });

        _db.Customers.Add(customer);
        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);

        return Created($"/api/customers/{customer.Id}?api-version=2026-11-12", CustomerResponse.From(customer));
    }

    /// <summary>List orders for a customer.</summary>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> ListOrders(Guid id, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersReadAll) is { } forbidden)
            return forbidden;

        if (!await _db.Customers.AnyAsync(customer => customer.Id == id, cancellationToken))
            return ApiSupport.Problem(this, new Error.NotFound(ResourceRef.For("Customer", id)) { Detail = "Customer not found." });

        var orders = await _db.Orders
            .Include(order => order.LineItems)
            .Where(order => order.CustomerId == id)
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderResponse.From).ToList();
    }
}

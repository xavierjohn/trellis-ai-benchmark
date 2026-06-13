namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Primitives;
using IResult = Microsoft.AspNetCore.Http.IResult;

[Route("api/customers")]
public sealed class CustomersController : ApiControllerBase
{
    private readonly OrderManagementService _service;
    private readonly IActorProvider _actorProvider;

    public CustomersController(OrderManagementService service, IActorProvider actorProvider)
    {
        _service = service;
        _actorProvider = actorProvider;
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.CustomersCreate, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);

        var parsed = ParseCustomer(request);
        if (parsed.Error is not null) return ToProblem(parsed.Error);

        parsed.TryGetValue(out var args);
        var result = await _service.CreateCustomerAsync(args!.FirstName, args.LastName, args.Email, args.PhoneNumber, args.ShippingAddress, cancellationToken);
        return ToHttp(result, CustomerResponse.From, StatusCodes.Status201Created, result.TryGetValue(out var customer) ? $"/api/customers/{(Guid)customer.Id}?api-version=2026-11-12" : null);
    }

    [HttpGet("{id:guid}/orders")]
    public async Task<IResult> ListOrders(Guid id, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(_actorProvider, Permissions.OrdersReadAll, cancellationToken);
        if (actor.Error is not null) return ToProblem(actor.Error);

        var customerId = CustomerId.TryCreate(id, "id");
        if (customerId.Error is not null) return ToProblem(customerId.Error);

        customerId.TryGetValue(out var parsedId);
        var result = await _service.ListOrdersByCustomerAsync(parsedId!, cancellationToken);
        return ToHttp(result, orders => orders.Select(OrderResponse.From).ToArray());
    }

    private static Result<(FirstName FirstName, LastName LastName, EmailAddress Email, Maybe<PhoneNumber> PhoneNumber, ShippingAddress ShippingAddress)> ParseCustomer(CreateCustomerRequest request)
    {
        var firstName = FirstName.TryCreate(request.FirstName, "firstName");
        if (firstName.Error is not null) return Result.Fail<(FirstName, LastName, EmailAddress, Maybe<PhoneNumber>, ShippingAddress)>(firstName.Error);
        var lastName = LastName.TryCreate(request.LastName, "lastName");
        if (lastName.Error is not null) return Result.Fail<(FirstName, LastName, EmailAddress, Maybe<PhoneNumber>, ShippingAddress)>(lastName.Error);
        var email = EmailAddress.TryCreate(request.Email, "email");
        if (email.Error is not null) return Result.Fail<(FirstName, LastName, EmailAddress, Maybe<PhoneNumber>, ShippingAddress)>(email.Error);
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? Result.Ok(Maybe<PhoneNumber>.None) : PhoneNumber.TryCreate(request.PhoneNumber, "phoneNumber").Map(Maybe.From);
        if (phone.Error is not null) return Result.Fail<(FirstName, LastName, EmailAddress, Maybe<PhoneNumber>, ShippingAddress)>(phone.Error);
        var address = ParseAddress(request.ShippingAddress);
        if (address.Error is not null) return Result.Fail<(FirstName, LastName, EmailAddress, Maybe<PhoneNumber>, ShippingAddress)>(address.Error);

        firstName.TryGetValue(out var fn);
        lastName.TryGetValue(out var ln);
        email.TryGetValue(out var em);
        phone.TryGetValue(out var ph);
        address.TryGetValue(out var addr);
        return Result.Ok((fn!, ln!, em!, ph, addr!));
    }

    private static Result<ShippingAddress> ParseAddress(ShippingAddressRequest? request)
    {
        if (request is null)
            return Result.Fail<ShippingAddress>(Error.InvalidInput.ForField("shippingAddress", "required", "Shipping address is required."));

        var street = Street.TryCreate(request.Street, "shippingAddress.street");
        if (street.Error is not null) return Result.Fail<ShippingAddress>(street.Error);
        var city = City.TryCreate(request.City, "shippingAddress.city");
        if (city.Error is not null) return Result.Fail<ShippingAddress>(city.Error);
        var state = StateProvince.TryCreate(request.State, "shippingAddress.state");
        if (state.Error is not null) return Result.Fail<ShippingAddress>(state.Error);
        var postalCode = PostalCode.TryCreate(request.PostalCode, "shippingAddress.postalCode");
        if (postalCode.Error is not null) return Result.Fail<ShippingAddress>(postalCode.Error);
        var country = CountryName.TryCreate(request.Country, "shippingAddress.country");
        if (country.Error is not null) return Result.Fail<ShippingAddress>(country.Error);

        street.TryGetValue(out var st);
        city.TryGetValue(out var ci);
        state.TryGetValue(out var sp);
        postalCode.TryGetValue(out var pc);
        country.TryGetValue(out var co);
        return Result.Ok(new ShippingAddress(st!, ci!, sp!, pc!, co!));
    }
}

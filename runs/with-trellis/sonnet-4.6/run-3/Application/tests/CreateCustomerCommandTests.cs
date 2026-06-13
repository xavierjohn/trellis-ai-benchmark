namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.Testing;

public class CreateCustomerCommandTests
{
    private readonly ISender _sender;
    private readonly TestActorProvider _actorProvider;

    public CreateCustomerCommandTests(ISender sender, TestActorProvider actorProvider)
    {
        _sender = sender;
        _actorProvider = actorProvider;
    }

    private static CreateCustomerCommand MakeCommand(string? email = null) => new(
        FirstName.TryCreate("Jane").Unwrap(),
        LastName.TryCreate("Doe").Unwrap(),
        Email.TryCreate(email ?? "jane@example.com").Unwrap(),
        Maybe<PhoneNumber>.None,
        ShippingAddress.TryCreate("123 Main St", "City", "State", "12345", "US").Unwrap());

    [Fact]
    public async Task Create_valid_customer_returns_success()
    {
        var result = await _sender.Send(MakeCommand(), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var customer = result.Unwrap();
        customer.Email.Value.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task Create_customer_without_permission_returns_forbidden()
    {
        await using var _ = _actorProvider.WithActor("restricted-user", Permissions.OrdersRead);
        var result = await _sender.Send(MakeCommand(), TestContext.Current.CancellationToken);
        result.Should().BeFailureOfType<Error.Forbidden>();
    }

    [Fact]
    public async Task Create_duplicate_email_returns_conflict()
    {
        (await _sender.Send(MakeCommand("dup@example.com"), TestContext.Current.CancellationToken))
            .Should().BeSuccess();

        var result = await _sender.Send(MakeCommand("dup@example.com"), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Conflict>();
    }
}

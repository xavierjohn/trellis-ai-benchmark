namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

public class CreateCustomerCommandTests
{
    private readonly ISender _sender;
    private readonly FakeRepository<Customer, CustomerId> _repo;

    public CreateCustomerCommandTests(ISender sender, FakeRepository<Customer, CustomerId> repo)
    {
        _sender = sender;
        _repo = repo;
    }

    private static ShippingAddress DefaultAddress() =>
        ShippingAddress.TryCreate("123 Main St", "Springfield", "IL", "62701", "US").GetValueOrThrow();

    [Fact]
    public async Task Create_valid_customer_returns_success()
    {
        var command = new CreateCustomerCommand(
            FirstName.Create("John"),
            LastName.Create("Doe"),
            EmailAddress.Create("john@example.com"),
            Maybe<PhoneNumber>.None,
            DefaultAddress());

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var customer = result.Unwrap();
        customer.FirstName.Value.Should().Be("John");
        customer.LastName.Value.Should().Be("Doe");
        customer.Email.Value.Should().Be("john@example.com");
        customer.Phone.Should().BeNone();
    }

    [Fact]
    public async Task Create_customer_with_phone_preserves_phone()
    {
        var phone = PhoneNumber.Create("+1-555-0100");
        var command = new CreateCustomerCommand(
            FirstName.Create("Jane"),
            LastName.Create("Smith"),
            EmailAddress.Create("jane@example.com"),
            Maybe.From(phone),
            DefaultAddress());

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Phone.Should().HaveValueEqualTo(phone);
    }

    [Fact]
    public async Task Create_duplicate_email_returns_conflict()
    {
        var email = EmailAddress.Create("duplicate@example.com");
        var command1 = new CreateCustomerCommand(
            FirstName.Create("First"),
            LastName.Create("User"),
            email,
            Maybe<PhoneNumber>.None,
            DefaultAddress());
        var command2 = new CreateCustomerCommand(
            FirstName.Create("Second"),
            LastName.Create("User"),
            email,
            Maybe<PhoneNumber>.None,
            DefaultAddress());

        _ = (await _sender.Send(command1, TestContext.Current.CancellationToken)).Unwrap();
        var result = await _sender.Send(command2, TestContext.Current.CancellationToken);

        result.Should().BeFailure();
        result.Error.Should().BeOfType<Error.Conflict>();
    }

    [Fact]
    public async Task Create_customer_is_persisted_in_repository()
    {
        var command = new CreateCustomerCommand(
            FirstName.Create("Persisted"),
            LastName.Create("Customer"),
            EmailAddress.Create("persisted@example.com"),
            Maybe<PhoneNumber>.None,
            DefaultAddress());

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var stored = await _repo.GetByIdAsync(result.Unwrap().Id, TestContext.Current.CancellationToken);
        stored.Should().BeSuccess();
    }
}

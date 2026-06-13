namespace Application.Tests;

using Mediator;
using OrderManagement.Application.Customers;
using OrderManagement.Domain;
using Trellis.Primitives;

public class CreateCustomerCommandTests
{
    private readonly ISender _sender;

    public CreateCustomerCommandTests(ISender sender) => _sender = sender;

    [Fact]
    public async Task Create_customer_with_duplicate_email_returns_conflict()
    {
        var address = ShippingAddress.TryCreate("1 Main St", "Seattle", "WA", "98101", "US").Unwrap();
        var command = new CreateCustomerCommand(
            FirstName.Create("Jane"),
            LastName.Create("Doe"),
            EmailAddress.Create("jane@example.com"),
            Maybe<PhoneNumber>.None,
            address);

        (await _sender.Send(command, TestContext.Current.CancellationToken)).Should().BeSuccess();
        var duplicate = await _sender.Send(command, TestContext.Current.CancellationToken);

        duplicate.Should().BeFailureOfType<Error.Conflict>();
    }
}

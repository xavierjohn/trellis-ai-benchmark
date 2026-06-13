namespace Domain.Tests;

using OrderManagement.Domain;

public class ShippingAddressTests
{
    [Fact]
    public void TryCreate_with_all_fields_returns_success()
    {
        var result = ShippingAddress.TryCreate("1 Main St", "Seattle", "WA", "98101", "US");

        result.Should().BeSuccess();
        result.Unwrap().Street.Should().Be("1 Main St");
    }

    [Fact]
    public void TryCreate_without_street_returns_invalid_input()
    {
        var result = ShippingAddress.TryCreate(null, "Seattle", "WA", "98101", "US");

        result.Should().BeFailureOfType<Error.InvalidInput>();
        result.Should().HaveErrorCode("invalid-input");
    }
}

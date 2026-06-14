using OrderManagement.Api.Domain.Customers;

namespace OrderManagement.Tests.Support;

public static class TestData
{
    public static ShippingAddress ValidAddress() =>
        new("123 Main St", "Springfield", "IL", "62701", "USA");

    public static Customer ValidCustomer(string email = "jane.doe@example.com") =>
        Customer.Create("Jane", "Doe", email, null, ValidAddress());
}

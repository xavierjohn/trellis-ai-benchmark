using NSubstitute;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Customers;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Tests.Application;

public class CustomerServiceTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CustomerService CreateService() => new(_customers, _uow);

    private static CreateCustomerRequest ValidRequest(string email = "jane@example.com") => new(
        "Jane", "Doe", email, null,
        new ShippingAddressDto("1 Main", "Town", "CA", "90001", "US"));

    [Fact]
    public async Task Create_WithPermission_Succeeds()
    {
        _customers.EmailExistsAsync(Arg.Any<string>()).Returns(false);
        var service = CreateService();

        var result = await service.CreateAsync(new Actor("a", [Permissions.CustomersCreate]), ValidRequest());

        Assert.True(result.IsSuccess);
        await _customers.Received(1).AddAsync(Arg.Any<Customer>());
    }

    [Fact]
    public async Task Create_WithoutPermission_Forbidden()
    {
        var service = CreateService();

        var result = await service.CreateAsync(new Actor("a", []), ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_Conflict()
    {
        _customers.EmailExistsAsync("jane@example.com").Returns(true);
        var service = CreateService();

        var result = await service.CreateAsync(new Actor("a", [Permissions.CustomersCreate]), ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Create_WithInvalidEmail_Validation()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            new Actor("a", [Permissions.CustomersCreate]), ValidRequest("not-an-email"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }
}

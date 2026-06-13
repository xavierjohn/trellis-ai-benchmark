namespace Application.Tests;

using Mediator;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

public class NotFoundTests(ISender sender, TestActorProvider actorProvider)
{
    [Fact]
    public async Task AddStock_to_missing_product_returns_not_found()
    {
        await using var _ = actorProvider.WithActor("wh-1", Permissions.ProductsManageStock);

        var result = await sender.Send(
            new AddStockCommand(ProductId.NewUniqueV7(), StockAddition.TryCreate(5).Unwrap()),
            TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public async Task GetOrder_missing_returns_not_found()
    {
        await using var _ = actorProvider.WithActor("user-1", Permissions.OrdersRead);

        var result = await sender.Send(
            new GetOrderByIdQuery(OrderId.NewUniqueV7()),
            TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    [Fact]
    public async Task CreateOrder_with_missing_customer_returns_not_found()
    {
        await using var _ = actorProvider.WithActor("sales-1", Permissions.OrdersCreate);

        var command = new CreateDraftOrderCommand(
            CustomerId.NewUniqueV7(),
            [new OrderLineRequest(ProductId.NewUniqueV7(), Quantity.TryCreate(1).Unwrap())]);

        var result = await sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.NotFound>();
    }
}

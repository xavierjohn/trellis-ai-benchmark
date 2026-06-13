namespace Application.Tests;

using Mediator;
using OrderManagement.Application;
using OrderManagement.Domain;
using Trellis.Primitives;
using Trellis.Testing;

public class AuthorizationTests(ISender sender, TestActorProvider actorProvider)
{
    private static CreateProductCommand ValidCreateProduct() =>
        new(
            ProductName.TryCreate("Widget").Unwrap(),
            Sku.TryCreate("WIDGET01").Unwrap(),
            MonetaryAmount.Create(9.99m));

    [Fact]
    public async Task Command_succeeds_with_required_permission()
    {
        await using var _ = actorProvider.WithActor("wh-1", Permissions.ProductsCreate);

        var result = await sender.Send(ValidCreateProduct(), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task Command_fails_with_forbidden_when_permission_missing()
    {
        await using var _ = actorProvider.WithActor("user-1", Permissions.OrdersRead);

        var result = await sender.Send(ValidCreateProduct(), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }
}

namespace OrderManagement.Application.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mediator;
using OrderManagement.Application;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Orders;
using OrderManagement.Domain;
using Trellis.Authorization;
using Trellis.Mediator;
using Trellis.Primitives;

/// <summary>Test resource loader backing the cancel-order ownership check.</summary>
internal sealed class TestOrderResourceLoader(IOrderRepository repository) : SharedResourceLoaderById<Order, OrderId>
{
    public override async Task<Result<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var maybe = await repository.FindByIdAsync(id, cancellationToken);
        return maybe.ToResult(new Error.NotFound(ResourceRef.For<Order>(id.Value)) { Detail = "Order not found." });
    }
}

/// <summary>
/// Builds an application-layer service provider wired exactly like production (mediator +
/// authorization behaviors) but backed by in-memory fakes and a controllable actor / clock.
/// </summary>
internal sealed class HandlerFixture
{
    public FakeCustomerRepository Customers { get; } = new();

    public FakeProductRepository Products { get; } = new();

    public FakeOrderRepository Orders { get; } = new();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public TestActorProvider ActorProvider { get; } = new(Actor.Create(
        ActorId.Create("admin"), new HashSet<string>(Permissions.All)));

    private readonly ServiceProvider _provider;

    public HandlerFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ICustomerRepository>(Customers);
        services.AddSingleton<IProductRepository>(Products);
        services.AddSingleton<IOrderRepository>(Orders);
        services.AddSingleton<IActorProvider>(ActorProvider);
        services.AddScoped<SharedResourceLoaderById<Order, OrderId>, TestOrderResourceLoader>();
        services.AddResourceAuthorization(typeof(CancelOrderCommand).Assembly);
        _provider = services.BuildServiceProvider();
    }

    public ISender Sender => _provider.GetRequiredService<ISender>();

    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> message) =>
        Sender.Send(message, TestContext.Current.CancellationToken);

    public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> message) =>
        Sender.Send(message, TestContext.Current.CancellationToken);

    public void ActAs(string actorId, params string[] permissions) =>
        ActorProvider.WithActor(Actor.Create(actorId, new HashSet<string>(permissions)));

    public void ActAsAdmin() =>
        ActorProvider.WithActor(Actor.Create("admin", new HashSet<string>(Permissions.All)));
}

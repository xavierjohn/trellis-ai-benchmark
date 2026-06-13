using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/orders", async (
            CreateOrderRequest request,
            OrderService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.CreateDraftAsync(actorProvider.GetCurrentActor(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Value.Id}", result.Value)
                : result.Error!.ToProblem();
        });

        group.MapPost("/orders/{id:guid}/line-items", async (
            Guid id,
            AddLineItemRequest request,
            OrderService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.AddLineItemAsync(actorProvider.GetCurrentActor(), id, request, ct);
            return result.ToOk();
        });

        group.MapDelete("/orders/{id:guid}/line-items/{lineItemId:guid}", async (
            Guid id,
            Guid lineItemId,
            OrderService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.RemoveLineItemAsync(actorProvider.GetCurrentActor(), id, lineItemId, ct);
            return result.ToOk();
        });

        group.MapPost("/orders/{id:guid}/submission", async (
            Guid id, OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.SubmitAsync(actorProvider.GetCurrentActor(), id, ct)).ToOk());

        group.MapPost("/orders/{id:guid}/approval", async (
            Guid id, OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.ApproveAsync(actorProvider.GetCurrentActor(), id, ct)).ToOk());

        group.MapPost("/orders/{id:guid}/shipment", async (
            Guid id, OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.ShipAsync(actorProvider.GetCurrentActor(), id, ct)).ToOk());

        group.MapPost("/orders/{id:guid}/delivery", async (
            Guid id, OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.DeliverAsync(actorProvider.GetCurrentActor(), id, ct)).ToOk());

        group.MapPost("/orders/{id:guid}/cancellation", async (
            Guid id, OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.CancelAsync(actorProvider.GetCurrentActor(), id, ct)).ToOk());

        group.MapGet("/orders/overdue", async (
            OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.ListOverdueAsync(actorProvider.GetCurrentActor(), ct)).ToOk());

        group.MapGet("/orders/{id:guid}", async (
            Guid id, OrderService service, IActorProvider actorProvider, CancellationToken ct) =>
            (await service.GetAsync(actorProvider.GetCurrentActor(), id, ct)).ToOk());

        return group;
    }
}

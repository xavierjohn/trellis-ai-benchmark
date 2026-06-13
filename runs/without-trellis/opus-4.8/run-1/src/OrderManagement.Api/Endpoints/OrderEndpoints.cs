using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/orders", async (CreateOrderRequest request, OrderService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return ApiResults.Created(result, o => $"/api/orders/{o.Id}");
        });

        group.MapPost("/orders/{id:guid}/line-items",
            async (Guid id, AddLineItemRequest request, OrderService service, CancellationToken ct) =>
            {
                var result = await service.AddLineItemAsync(id, request, ct);
                return ApiResults.Ok(result);
            });

        group.MapDelete("/orders/{id:guid}/line-items/{lineItemId:guid}",
            async (Guid id, Guid lineItemId, OrderService service, CancellationToken ct) =>
            {
                var result = await service.RemoveLineItemAsync(id, lineItemId, ct);
                return ApiResults.Ok(result);
            });

        group.MapPost("/orders/{id:guid}/submission", async (Guid id, OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.SubmitAsync(id, ct)));

        group.MapPost("/orders/{id:guid}/approval", async (Guid id, OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.ApproveAsync(id, ct)));

        group.MapPost("/orders/{id:guid}/shipment", async (Guid id, OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.ShipAsync(id, ct)));

        group.MapPost("/orders/{id:guid}/delivery", async (Guid id, OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.DeliverAsync(id, ct)));

        group.MapPost("/orders/{id:guid}/cancellation", async (Guid id, OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.CancelAsync(id, ct)));

        group.MapGet("/orders/overdue", async (OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.ListOverdueAsync(ct)));

        group.MapGet("/orders/{id:guid}", async (Guid id, OrderService service, CancellationToken ct) =>
            ApiResults.Ok(await service.GetByIdAsync(id, ct)));
    }
}

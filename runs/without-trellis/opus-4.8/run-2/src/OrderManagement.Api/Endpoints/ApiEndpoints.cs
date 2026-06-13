using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Products;

namespace OrderManagement.Api.Endpoints;

public static class ApiEndpoints
{
    private static string Loc(string path) => $"{path}?api-version={ApiConstants.Version}";

    public static void MapApiEndpoints(this RouteGroupBuilder api)
    {
        MapCustomers(api);
        MapProducts(api);
        MapOrders(api);
    }

    private static void MapCustomers(RouteGroupBuilder api)
    {
        api.MapPost("/customers", async (
            CreateCustomerRequest request, IActor actor, CustomerService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(actor, request, ct);
            return result.ToCreated(c => Loc($"/api/customers/{c.Id}"));
        });

        api.MapGet("/customers/{id:guid}/orders", async (
            Guid id, IActor actor, OrderQueryService service, CancellationToken ct) =>
        {
            var result = await service.ListByCustomerAsync(actor, id, ct);
            return result.ToOk();
        });
    }

    private static void MapProducts(RouteGroupBuilder api)
    {
        api.MapPost("/products", async (
            CreateProductRequest request, IActor actor, ProductService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(actor, request, ct);
            return result.ToCreated(p => Loc($"/api/products/{p.Id}"));
        });

        api.MapPost("/products/{id:guid}/stock-additions", async (
            Guid id, AddStockRequest request, IActor actor, ProductService service, CancellationToken ct) =>
        {
            var result = await service.AddStockAsync(actor, id, request, ct);
            return result.ToOk();
        });
    }

    private static void MapOrders(RouteGroupBuilder api)
    {
        api.MapPost("/orders", async (
            CreateOrderRequest request, IActor actor, OrderService service, CancellationToken ct) =>
        {
            var result = await service.CreateDraftAsync(actor, request, ct);
            return result.ToCreated(o => Loc($"/api/orders/{o.Id}"));
        });

        api.MapPost("/orders/{id:guid}/line-items", async (
            Guid id, AddLineItemRequest request, IActor actor, OrderService service, CancellationToken ct) =>
        {
            var result = await service.AddLineItemAsync(actor, id, request, ct);
            return result.ToOk();
        });

        api.MapDelete("/orders/{id:guid}/line-items/{lineItemId:guid}", async (
            Guid id, Guid lineItemId, IActor actor, OrderService service, CancellationToken ct) =>
        {
            var result = await service.RemoveLineItemAsync(actor, id, lineItemId, ct);
            return result.ToOk();
        });

        api.MapPost("/orders/{id:guid}/submission", async (
            Guid id, IActor actor, OrderService service, CancellationToken ct) =>
            (await service.SubmitAsync(actor, id, ct)).ToOk());

        api.MapPost("/orders/{id:guid}/approval", async (
            Guid id, IActor actor, OrderService service, CancellationToken ct) =>
            (await service.ApproveAsync(actor, id, ct)).ToOk());

        api.MapPost("/orders/{id:guid}/shipment", async (
            Guid id, IActor actor, OrderService service, CancellationToken ct) =>
            (await service.ShipAsync(actor, id, ct)).ToOk());

        api.MapPost("/orders/{id:guid}/delivery", async (
            Guid id, IActor actor, OrderService service, CancellationToken ct) =>
            (await service.DeliverAsync(actor, id, ct)).ToOk());

        api.MapPost("/orders/{id:guid}/cancellation", async (
            Guid id, IActor actor, OrderService service, CancellationToken ct) =>
            (await service.CancelAsync(actor, id, ct)).ToOk());

        api.MapGet("/orders/overdue", async (
            IActor actor, OrderQueryService service, CancellationToken ct) =>
            (await service.ListOverdueAsync(actor, ct)).ToOk());

        api.MapGet("/orders/{id:guid}", async (
            Guid id, IActor actor, OrderQueryService service, CancellationToken ct) =>
            (await service.GetByIdAsync(actor, id, ct)).ToOk());
    }
}

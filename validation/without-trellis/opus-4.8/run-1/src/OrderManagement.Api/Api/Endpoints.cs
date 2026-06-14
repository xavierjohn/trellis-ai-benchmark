using OrderManagement.Api.Application;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Api.Application.Customers;
using OrderManagement.Api.Application.Orders;
using OrderManagement.Api.Application.Products;
using OrderManagement.Api.Contracts;

namespace OrderManagement.Api.Api;

public static class Endpoints
{
    public static void MapOrderManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var api = app.MapGroup("/api");

        // ----- Customers -----
        api.MapPost("/customers", async (
            CreateCustomerRequest request,
            IActorProvider actors,
            CustomerService service,
            CancellationToken ct) =>
        {
            var customer = await service.CreateAsync(actors.GetCurrentActor(), request, ct);
            return Results.Created($"/api/customers/{customer.Id}", customer.ToResponse());
        });

        api.MapGet("/customers/{id:guid}/orders", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var orders = await service.ListByCustomerAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(orders.Select(o => o.ToResponse()).ToList());
        });

        // ----- Products -----
        api.MapPost("/products", async (
            CreateProductRequest request,
            IActorProvider actors,
            ProductService service,
            CancellationToken ct) =>
        {
            var product = await service.CreateAsync(actors.GetCurrentActor(), request, ct);
            return Results.Created($"/api/products/{product.Id}", product.ToResponse());
        });

        api.MapPost("/products/{id:guid}/stock-additions", async (
            Guid id,
            AddStockRequest request,
            IActorProvider actors,
            ProductService service,
            CancellationToken ct) =>
        {
            var product = await service.AddStockAsync(actors.GetCurrentActor(), id, request, ct);
            return Results.Ok(product.ToResponse());
        });

        // ----- Orders -----
        api.MapPost("/orders", async (
            CreateOrderRequest request,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.CreateDraftAsync(actors.GetCurrentActor(), request, ct);
            return Results.Created($"/api/orders/{order.Id}", order.ToResponse());
        });

        api.MapPost("/orders/{id:guid}/line-items", async (
            Guid id,
            AddLineItemRequest request,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.AddLineItemAsync(actors.GetCurrentActor(), id, request, ct);
            return Results.Ok(order.ToResponse());
        });

        api.MapDelete("/orders/{id:guid}/line-items/{lineItemId:guid}", async (
            Guid id,
            Guid lineItemId,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.RemoveLineItemAsync(actors.GetCurrentActor(), id, lineItemId, ct);
            return Results.Ok(order.ToResponse());
        });

        api.MapPost("/orders/{id:guid}/submission", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.SubmitAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(order.ToResponse());
        });

        api.MapPost("/orders/{id:guid}/approval", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.ApproveAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(order.ToResponse());
        });

        api.MapPost("/orders/{id:guid}/shipment", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.ShipAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(order.ToResponse());
        });

        api.MapPost("/orders/{id:guid}/delivery", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.DeliverAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(order.ToResponse());
        });

        api.MapPost("/orders/{id:guid}/cancellation", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.CancelAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(order.ToResponse());
        });

        // Overdue must be mapped distinctly from /orders/{id}.
        api.MapGet("/orders/overdue", async (
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var orders = await service.ListOverdueAsync(actors.GetCurrentActor(), ct);
            return Results.Ok(orders.Select(o => o.ToResponse()).ToList());
        });

        api.MapGet("/orders/{id:guid}", async (
            Guid id,
            IActorProvider actors,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.GetByIdAsync(actors.GetCurrentActor(), id, ct);
            return Results.Ok(order.ToResponse());
        });
    }
}

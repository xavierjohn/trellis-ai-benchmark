using OrderManagement.Api.Auth;
using OrderManagement.Api.Models;
using OrderManagement.Api.Responses;
using OrderManagement.Api.Services;

namespace OrderManagement.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/orders", async (
            HttpContext httpContext,
            CreateOrderRequest request,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.CreateDraftOrder(actor, request);
            var response = order.ToResponse();
            return Results.Created($"/api/orders/{order.Id}", response);
        });

        app.MapPost("/api/orders/{id}/line-items", async (
            Guid id,
            HttpContext httpContext,
            AddLineItemRequest request,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.AddLineItem(actor, id, request);
            return Results.Ok(order.ToResponse());
        });

        app.MapDelete("/api/orders/{id}/line-items/{lineItemId}", async (
            Guid id,
            Guid lineItemId,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.RemoveLineItem(actor, id, lineItemId);
            return Results.Ok(order.ToResponse());
        });

        app.MapPost("/api/orders/{id}/submission", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.SubmitOrder(actor, id);
            return Results.Ok(order.ToResponse());
        });

        app.MapPost("/api/orders/{id}/approval", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.ApproveOrder(actor, id);
            return Results.Ok(order.ToResponse());
        });

        app.MapPost("/api/orders/{id}/shipment", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.ShipOrder(actor, id);
            return Results.Ok(order.ToResponse());
        });

        app.MapPost("/api/orders/{id}/delivery", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.DeliverOrder(actor, id);
            return Results.Ok(order.ToResponse());
        });

        app.MapPost("/api/orders/{id}/cancellation", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.CancelOrder(actor, id);
            return Results.Ok(order.ToResponse());
        });

        // GET /api/orders/overdue must be registered BEFORE /api/orders/{id}
        // to avoid "overdue" being treated as a GUID
        app.MapGet("/api/orders/overdue", async (
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var orders = await orderService.GetOverdueOrders(actor);
            return Results.Ok(orders.Select(o => o.ToResponse()).ToList());
        });

        app.MapGet("/api/orders/{id}", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var order = await orderService.GetOrderById(actor, id);
            return Results.Ok(order.ToResponse());
        });
    }
}

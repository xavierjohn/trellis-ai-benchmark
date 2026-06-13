using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Middleware;
using OrderManagement.Api.Models;
using OrderManagement.Application.Commands;
using OrderManagement.Application.Queries;

namespace OrderManagement.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders", CreateDraftOrder).RequiresApiVersion();
        app.MapPost("/api/orders/{id}/line-items", AddLineItem).RequiresApiVersion();
        app.MapDelete("/api/orders/{id}/line-items/{lineItemId}", RemoveLineItem).RequiresApiVersion();
        app.MapPost("/api/orders/{id}/submission", SubmitOrder).RequiresApiVersion();
        app.MapPost("/api/orders/{id}/approval", ApproveOrder).RequiresApiVersion();
        app.MapPost("/api/orders/{id}/shipment", ShipOrder).RequiresApiVersion();
        app.MapPost("/api/orders/{id}/delivery", DeliverOrder).RequiresApiVersion();
        app.MapPost("/api/orders/{id}/cancellation", CancelOrder).RequiresApiVersion();
        app.MapGet("/api/orders/overdue", GetOverdueOrders).RequiresApiVersion();
        app.MapGet("/api/orders/{id}", GetOrderById).RequiresApiVersion();
        return app;
    }

    private static async Task<IResult> CreateDraftOrder(
        [FromBody] CreateOrderRequest req,
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var lineItems = (req.LineItems ?? [])
            .Select(li => new LineItemInput(li.ProductId, li.Quantity))
            .ToList();

        var command = new CreateDraftOrderCommand(
            actor,
            req.CustomerId ?? Guid.Empty,
            lineItems,
            timeProvider.GetUtcNow());

        var result = await mediator.Send(command, ct);
        return Results.Created($"/api/orders/{result.Id}", result);
    }

    private static async Task<IResult> AddLineItem(
        Guid id,
        [FromBody] AddLineItemRequest req,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new AddLineItemCommand(actor, id, req.ProductId, req.Quantity);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> RemoveLineItem(
        Guid id,
        Guid lineItemId,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new RemoveLineItemCommand(actor, id, lineItemId);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SubmitOrder(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new SubmitOrderCommand(actor, id, timeProvider.GetUtcNow());
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ApproveOrder(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new ApproveOrderCommand(actor, id, timeProvider.GetUtcNow());
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ShipOrder(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new ShipOrderCommand(actor, id, timeProvider.GetUtcNow());
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeliverOrder(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new DeliverOrderCommand(actor, id, timeProvider.GetUtcNow());
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CancelOrder(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new CancelOrderCommand(actor, id, timeProvider.GetUtcNow());
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOrderById(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var query = new GetOrderByIdQuery(actor, id);
        var result = await mediator.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOverdueOrders(
        HttpContext ctx,
        IMediator mediator,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var query = new ListOverdueOrdersQuery(actor, timeProvider.GetUtcNow());
        var result = await mediator.Send(query, ct);
        return Results.Ok(result);
    }
}

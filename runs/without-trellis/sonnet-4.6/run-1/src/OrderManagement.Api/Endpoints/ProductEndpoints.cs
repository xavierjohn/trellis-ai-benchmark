using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Middleware;
using OrderManagement.Api.Models;
using OrderManagement.Application.Commands;

namespace OrderManagement.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/products", CreateProduct).RequiresApiVersion();
        app.MapPost("/api/products/{id}/stock-additions", AddStock).RequiresApiVersion();
        return app;
    }

    private static async Task<IResult> CreateProduct(
        [FromBody] CreateProductRequest req,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new CreateProductCommand(
            actor,
            req.ProductName ?? "",
            req.SKU ?? "",
            req.UnitPrice ?? 0);

        var result = await mediator.Send(command, ct);
        return Results.Created($"/api/products/{result.Id}", result);
    }

    private static async Task<IResult> AddStock(
        Guid id,
        [FromBody] AddStockRequest req,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new AddStockCommand(actor, id, req.Quantity);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Middleware;
using OrderManagement.Api.Models;
using OrderManagement.Application.Commands;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customers", CreateCustomer)
            .RequiresApiVersion();
        app.MapGet("/api/customers/{id}/orders", ListOrdersByCustomer)
            .RequiresApiVersion();
        return app;
    }

    private static async Task<IResult> CreateCustomer(
        [FromBody] CreateCustomerRequest req,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var command = new CreateCustomerCommand(
            actor,
            req.FirstName ?? "",
            req.LastName ?? "",
            req.Email ?? "",
            req.PhoneNumber,
            req.Street ?? "",
            req.City ?? "",
            req.State ?? "",
            req.PostalCode ?? "",
            req.Country ?? "");

        var result = await mediator.Send(command, ct);
        return Results.Created($"/api/customers/{result.Id}", result);
    }

    private static async Task<IResult> ListOrdersByCustomer(
        Guid id,
        HttpContext ctx,
        IMediator mediator,
        CancellationToken ct)
    {
        var actor = ctx.GetActor();
        var query = new Application.Queries.ListOrdersByCustomerQuery(actor, id);
        var result = await mediator.Send(query, ct);
        return Results.Ok(result);
    }
}

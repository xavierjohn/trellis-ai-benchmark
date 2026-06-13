using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/customers", async (
            CreateCustomerRequest request,
            CustomerService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(actorProvider.GetCurrentActor(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/customers/{result.Value.Id}", result.Value)
                : result.Error!.ToProblem();
        });

        group.MapGet("/customers/{id:guid}/orders", async (
            Guid id,
            OrderService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.ListByCustomerAsync(actorProvider.GetCurrentActor(), id, ct);
            return result.ToOk();
        });

        return group;
    }
}

using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/customers", async (CreateCustomerRequest request, CustomerService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return ApiResults.Created(result, c => $"/api/customers/{c.Id}");
        });

        group.MapGet("/customers/{id:guid}/orders", async (Guid id, OrderService service, CancellationToken ct) =>
        {
            var result = await service.ListByCustomerAsync(id, ct);
            return ApiResults.Ok(result);
        });
    }
}

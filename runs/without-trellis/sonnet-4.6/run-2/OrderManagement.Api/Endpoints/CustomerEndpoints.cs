using OrderManagement.Api.Auth;
using OrderManagement.Api.Models;
using OrderManagement.Api.Responses;
using OrderManagement.Api.Services;

namespace OrderManagement.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        app.MapPost("/api/customers", async (
            HttpContext httpContext,
            CreateCustomerRequest request,
            CustomerService customerService) =>
        {
            var actor = httpContext.GetActor();
            var customer = await customerService.CreateCustomer(actor, request);
            var response = customer.ToResponse();
            return Results.Created($"/api/customers/{customer.Id}", response);
        });

        app.MapGet("/api/customers/{id}/orders", async (
            Guid id,
            HttpContext httpContext,
            OrderService orderService) =>
        {
            var actor = httpContext.GetActor();
            var orders = await orderService.GetOrdersByCustomer(actor, id);
            return Results.Ok(orders.Select(o => o.ToResponse()).ToList());
        });
    }
}

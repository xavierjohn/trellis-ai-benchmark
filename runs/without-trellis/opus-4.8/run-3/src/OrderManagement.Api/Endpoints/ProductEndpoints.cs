using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Products;

namespace OrderManagement.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/products", async (
            CreateProductRequest request,
            ProductService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(actorProvider.GetCurrentActor(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/products/{result.Value.Id}", result.Value)
                : result.Error!.ToProblem();
        });

        group.MapPost("/products/{id:guid}/stock-additions", async (
            Guid id,
            AddStockRequest request,
            ProductService service,
            IActorProvider actorProvider,
            CancellationToken ct) =>
        {
            var result = await service.AddStockAsync(actorProvider.GetCurrentActor(), id, request, ct);
            return result.ToOk();
        });

        return group;
    }
}

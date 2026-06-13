using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Products;

namespace OrderManagement.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/products", async (CreateProductRequest request, ProductService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return ApiResults.Created(result, p => $"/api/products/{p.Id}");
        });

        group.MapPost("/products/{id:guid}/stock-additions",
            async (Guid id, AddStockRequest request, ProductService service, CancellationToken ct) =>
            {
                var result = await service.AddStockAsync(id, request, ct);
                return ApiResults.Ok(result);
            });
    }
}

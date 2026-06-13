using OrderManagement.Api.Auth;
using OrderManagement.Api.Models;
using OrderManagement.Api.Responses;
using OrderManagement.Api.Services;

namespace OrderManagement.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        app.MapPost("/api/products", async (
            HttpContext httpContext,
            CreateProductRequest request,
            ProductService productService) =>
        {
            var actor = httpContext.GetActor();
            var product = await productService.CreateProduct(actor, request);
            var response = product.ToResponse();
            return Results.Created($"/api/products/{product.Id}", response);
        });

        app.MapPost("/api/products/{id}/stock-additions", async (
            Guid id,
            HttpContext httpContext,
            AddStockRequest request,
            ProductService productService) =>
        {
            var actor = httpContext.GetActor();
            var product = await productService.AddStock(actor, id, request);
            return Results.Ok(product.ToResponse());
        });
    }
}

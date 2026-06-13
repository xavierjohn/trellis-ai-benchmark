using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=OrderManagement.db"));
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<OrderManagementService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

var api = app.MapGroup("/api").AddEndpointFilter(ApiVersionFilter);

api.MapPost("/customers", async (HttpContext http, OrderManagementService service, CreateCustomerRequest request, CancellationToken ct) =>
{
    var result = await service.CreateCustomerAsync(GetActor(http), request, ct);
    return ToHttpResult(result, c => Results.Created($"/api/customers/{c.Id}?api-version=2026-11-12", c.ToResponse()));
});

api.MapPost("/products", async (HttpContext http, OrderManagementService service, CreateProductRequest request, CancellationToken ct) =>
{
    var result = await service.CreateProductAsync(GetActor(http), request, ct);
    return ToHttpResult(result, p => Results.Created($"/api/products/{p.Id}?api-version=2026-11-12", p.ToResponse()));
});

api.MapPost("/products/{id:guid}/stock-additions", async (HttpContext http, OrderManagementService service, Guid id, AddStockRequest request, CancellationToken ct) =>
{
    var result = await service.AddStockAsync(GetActor(http), id, request, ct);
    return ToHttpResult(result, p => Results.Ok(p.ToResponse()));
});

api.MapPost("/orders", async (HttpContext http, OrderManagementService service, CreateOrderRequest request, CancellationToken ct) =>
{
    var result = await service.CreateOrderAsync(GetActor(http), request, ct);
    return ToHttpResult(result, o => Results.Created($"/api/orders/{o.Id}?api-version=2026-11-12", o.ToResponse()));
});

api.MapPost("/orders/{id:guid}/line-items", async (HttpContext http, OrderManagementService service, Guid id, AddLineItemRequest request, CancellationToken ct) =>
{
    var result = await service.AddLineItemAsync(GetActor(http), id, request, ct);
    return ToHttpResult(result, o => Results.Ok(o.ToResponse()));
});

api.MapDelete("/orders/{id:guid}/line-items/{lineItemId:guid}", async (HttpContext http, OrderManagementService service, Guid id, Guid lineItemId, CancellationToken ct) =>
{
    var result = await service.RemoveLineItemAsync(GetActor(http), id, lineItemId, ct);
    return ToHttpResult(result, o => Results.Ok(o.ToResponse()));
});

api.MapPost("/orders/{id:guid}/submission", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.SubmitAsync(GetActor(http), id, ct), o => Results.Ok(o.ToResponse())));

api.MapPost("/orders/{id:guid}/approval", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.ApproveAsync(GetActor(http), id, ct), o => Results.Ok(o.ToResponse())));

api.MapPost("/orders/{id:guid}/shipment", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.ShipAsync(GetActor(http), id, ct), o => Results.Ok(o.ToResponse())));

api.MapPost("/orders/{id:guid}/delivery", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.DeliverAsync(GetActor(http), id, ct), o => Results.Ok(o.ToResponse())));

api.MapPost("/orders/{id:guid}/cancellation", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.CancelAsync(GetActor(http), id, ct), o => Results.Ok(o.ToResponse())));

api.MapGet("/orders/overdue", async (HttpContext http, OrderManagementService service, CancellationToken ct) =>
    ToHttpResult(await service.ListOverdueAsync(GetActor(http), ct), orders => Results.Ok(orders.Select(o => o.ToResponse()))));

api.MapGet("/orders/{id:guid}", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.GetOrderAsync(GetActor(http), id, ct), o => Results.Ok(o.ToResponse())));

api.MapGet("/customers/{id:guid}/orders", async (HttpContext http, OrderManagementService service, Guid id, CancellationToken ct) =>
    ToHttpResult(await service.ListOrdersByCustomerAsync(GetActor(http), id, ct), orders => Results.Ok(orders.Select(o => o.ToResponse()))));

app.Run();

static ValueTask<object?> ApiVersionFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
{
    var http = context.HttpContext;
    if (!http.Request.Query.TryGetValue("api-version", out var version) || version != "2026-11-12")
    {
        return ValueTask.FromResult<object?>(Results.Problem(new ProblemDetails
        {
            Title = "Invalid API version",
            Detail = "Requests must include ?api-version=2026-11-12.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        }));
    }

    return next(context);
}

static Actor GetActor(HttpContext http)
{
    if (http.Request.Headers.TryGetValue("X-Test-Actor", out var header) && !string.IsNullOrWhiteSpace(header))
    {
        var actor = JsonSerializer.Deserialize<TestActor>(header!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return new Actor(actor?.Id ?? "anonymous", (actor?.Permissions ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    var id = http.User.FindFirstValue("sub") ?? http.User.FindFirstValue("oid");
    var permissions = http.User.FindAll(ClaimTypes.Role).Concat(http.User.FindAll("role")).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    return !string.IsNullOrWhiteSpace(id) ? new Actor(id, permissions) : Actor.Admin;
}

static IResult ToHttpResult<T>(AppResult<T> result, Func<T, IResult> onSuccess)
{
    if (result.IsSuccess) return onSuccess(result.Value!);
    var error = result.Error!;
    var status = error.Kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
    var title = error.Kind switch
    {
        ErrorKind.Validation => "Validation error",
        ErrorKind.NotFound => "Not found",
        ErrorKind.Conflict => "Conflict",
        ErrorKind.Forbidden => "Forbidden",
        _ => "Error"
    };
    var problem = new ProblemDetails
    {
        Title = title,
        Detail = error.Message,
        Status = status,
        Type = $"https://httpstatuses.com/{status}"
    };
    if (error.Errors is not null) problem.Extensions["errors"] = error.Errors;
    return Results.Problem(problem);
}

internal sealed record TestActor(string Id, string[] Permissions);

public partial class Program;

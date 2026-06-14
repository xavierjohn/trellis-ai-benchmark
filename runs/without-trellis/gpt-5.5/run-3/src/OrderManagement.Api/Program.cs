using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddDbContext<OrderManagementDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=OrderManagement.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorProvider, HeaderActorProvider>();
builder.Services.AddScoped<OrderManagementService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (status, title, detail, errors) = exception switch
        {
            ValidationProblemException ex => (StatusCodes.Status422UnprocessableEntity, "Validation error", ex.Message, ex.Errors),
            NotFoundProblemException ex => (StatusCodes.Status404NotFound, "Not found", ex.Message, null),
            ConflictProblemException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message, null),
            ForbiddenProblemException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message, null),
            BadHttpRequestException ex => (StatusCodes.Status400BadRequest, "Bad request", ex.Message, null),
            JsonException ex => (StatusCodes.Status400BadRequest, "Bad request", ex.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.", null)
        };
        context.Response.StatusCode = status;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        var problem = new ValidationProblemDetails(errors ?? new Dictionary<string, string[]>())
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    });
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") && !context.Request.Query.ContainsKey("api-version"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad request",
            Detail = "The api-version query parameter is required.",
            Type = "https://httpstatuses.com/400",
            Instance = context.Request.Path
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return;
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
var api = app.MapGroup("/api");

api.MapPost("/customers", async (CreateCustomerRequest request, OrderManagementService service, HttpContext http) =>
{
    var customer = await service.CreateCustomerAsync(request, http.RequestAborted);
    return Results.Created($"/api/customers/{customer.Id}", customer);
});
api.MapPost("/products", async (CreateProductRequest request, OrderManagementService service, HttpContext http) =>
{
    var product = await service.CreateProductAsync(request, http.RequestAborted);
    return Results.Created($"/api/products/{product.Id}", product);
});
api.MapPost("/products/{id:guid}/stock-additions", async (Guid id, AddStockRequest request, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.AddStockAsync(id, request, http.RequestAborted)));
api.MapPost("/orders", async (CreateOrderRequest request, OrderManagementService service, HttpContext http) =>
{
    var order = await service.CreateOrderAsync(request, http.RequestAborted);
    return Results.Created($"/api/orders/{order.Id}", order);
});
api.MapPost("/orders/{id:guid}/line-items", async (Guid id, AddLineItemRequest request, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.AddLineItemAsync(id, request, http.RequestAborted)));
api.MapDelete("/orders/{id:guid}/line-items/{lineItemId:guid}", async (Guid id, Guid lineItemId, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.RemoveLineItemAsync(id, lineItemId, http.RequestAborted)));
api.MapPost("/orders/{id:guid}/submission", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.SubmitOrderAsync(id, http.RequestAborted)));
api.MapPost("/orders/{id:guid}/approval", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.ApproveOrderAsync(id, http.RequestAborted)));
api.MapPost("/orders/{id:guid}/shipment", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.ShipOrderAsync(id, http.RequestAborted)));
api.MapPost("/orders/{id:guid}/delivery", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.DeliverOrderAsync(id, http.RequestAborted)));
api.MapPost("/orders/{id:guid}/cancellation", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.CancelOrderAsync(id, http.RequestAborted)));
api.MapGet("/orders/overdue", async (OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.ListOverdueOrdersAsync(http.RequestAborted)));
api.MapGet("/orders/{id:guid}", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.GetOrderAsync(id, http.RequestAborted)));
api.MapGet("/customers/{id:guid}/orders", async (Guid id, OrderManagementService service, HttpContext http) =>
    Results.Ok(await service.ListOrdersByCustomerAsync(id, http.RequestAborted)));

app.Run();

public partial class Program;

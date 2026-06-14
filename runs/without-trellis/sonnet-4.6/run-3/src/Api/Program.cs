using Application.Common;
using Application.Customers;
using Application.Interfaces;
using Application.Orders;
using Application.Products;
using Domain.Common;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=OrderManagement.db"));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<AddStockHandler>();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<AddLineItemHandler>();
builder.Services.AddScoped<RemoveLineItemHandler>();
builder.Services.AddScoped<SubmitOrderHandler>();
builder.Services.AddScoped<ApproveOrderHandler>();
builder.Services.AddScoped<ShipOrderHandler>();
builder.Services.AddScoped<DeliverOrderHandler>();
builder.Services.AddScoped<CancelOrderHandler>();
builder.Services.AddScoped<GetOrderHandler>();
builder.Services.AddScoped<GetCustomerOrdersHandler>();
builder.Services.AddScoped<GetOverdueOrdersHandler>();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapHealthChecks("/health");

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !context.Request.Query.ContainsKey("api-version"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "Bad Request",
            status = 400,
            detail = "The 'api-version' query parameter is required."
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        return;
    }

    await next();
});

app.MapPost("/api/customers", async (CreateCustomerRequest req, CreateCustomerHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var command = new CreateCustomerCommand(
        req.FirstName,
        req.LastName,
        req.Email,
        req.PhoneNumber,
        req.Street,
        req.City,
        req.State,
        req.PostalCode,
        req.Country);

    var result = await handler.HandleAsync(command, actor, ct);
    return ToHttpResult(result, customer =>
        Results.Created($"/api/customers/{customer.CustomerId}?api-version=2026-11-12", MapCustomer(customer)));
});

app.MapPost("/api/products", async (CreateProductRequest req, CreateProductHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var command = new CreateProductCommand(req.ProductName, req.SKU, req.UnitPrice, req.StockQuantity);
    var result = await handler.HandleAsync(command, actor, ct);
    return ToHttpResult(result, product =>
        Results.Created($"/api/products/{product.ProductId}?api-version=2026-11-12", MapProduct(product)));
});

app.MapPost("/api/products/{id:guid}/stock-additions", async (Guid id, AddStockRequest req, AddStockHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(new AddStockCommand(id, req.Quantity), actor, ct);
    return ToHttpResult(result, product => Results.Ok(MapProduct(product)));
});

app.MapPost("/api/orders", async (CreateOrderRequest req, CreateOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(new CreateOrderCommand(req.CustomerId), actor, ct);
    return ToHttpResult(result, order =>
        Results.Created($"/api/orders/{order.OrderId}?api-version=2026-11-12", MapOrder(order)));
});

app.MapPost("/api/orders/{id:guid}/line-items", async (Guid id, AddLineItemRequest req, AddLineItemHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(new AddLineItemCommand(id, req.ProductId, req.Quantity), actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapDelete("/api/orders/{id:guid}/line-items/{lineItemId:guid}", async (Guid id, Guid lineItemId, RemoveLineItemHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(new RemoveLineItemCommand(id, lineItemId), actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapPost("/api/orders/{id:guid}/submission", async (Guid id, SubmitOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapPost("/api/orders/{id:guid}/approval", async (Guid id, ApproveOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapPost("/api/orders/{id:guid}/shipment", async (Guid id, ShipOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapPost("/api/orders/{id:guid}/delivery", async (Guid id, DeliverOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapPost("/api/orders/{id:guid}/cancellation", async (Guid id, CancelOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapGet("/api/orders/{id:guid}", async (Guid id, GetOrderHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, order => Results.Ok(MapOrder(order)));
});

app.MapGet("/api/customers/{id:guid}/orders", async (Guid id, GetCustomerOrdersHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(id, actor, ct);
    return ToHttpResult(result, orders => Results.Ok(orders.Select(MapOrder)));
});

app.MapGet("/api/orders/overdue", async (GetOverdueOrdersHandler handler, HttpContext ctx, CancellationToken ct) =>
{
    var actor = GetActor(ctx);
    var result = await handler.HandleAsync(actor, ct);
    return ToHttpResult(result, orders => Results.Ok(orders.Select(MapOrder)));
});

app.Run();

static Actor GetActor(HttpContext ctx)
{
    var header = ctx.Request.Headers["X-Test-Actor"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(header))
    {
        return Actor.Admin();
    }

    try
    {
        var dto = JsonSerializer.Deserialize<ActorDto>(
            header,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return dto is null ? Actor.Admin() : new Actor(dto.Id, dto.Permissions);
    }
    catch
    {
        return Actor.Admin();
    }
}

static IResult ToHttpResult<T>(Result<T> result, Func<T, IResult> onSuccess)
{
    if (result.IsSuccess)
    {
        return onSuccess(result.Value!);
    }

    return result.ErrorCode switch
    {
        "not_found" => Results.Problem(
            title: "Not Found",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
        "forbidden" => Results.Problem(
            title: "Forbidden",
            detail: result.Error,
            statusCode: StatusCodes.Status403Forbidden,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
        "conflict" => Results.Problem(
            title: "Conflict",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
        _ => Results.Problem(
            title: "Unprocessable Content",
            detail: result.Error,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.22")
    };
}

static object MapCustomer(Domain.Customers.Customer customer) => new
{
    customer.CustomerId,
    customer.FirstName,
    customer.LastName,
    customer.Email,
    customer.PhoneNumber,
    ShippingAddress = new
    {
        customer.ShippingAddress.Street,
        customer.ShippingAddress.City,
        customer.ShippingAddress.State,
        customer.ShippingAddress.PostalCode,
        customer.ShippingAddress.Country
    }
};

static object MapProduct(Domain.Products.Product product) => new
{
    product.ProductId,
    product.ProductName,
    product.SKU,
    product.UnitPrice,
    product.StockQuantity
};

static object MapOrder(Domain.Orders.Order order) => new
{
    order.OrderId,
    order.CustomerId,
    order.CreatedByActorId,
    Status = order.Status.ToString(),
    order.CreatedAt,
    order.SubmittedAt,
    order.ShippedAt,
    LineItems = order.LineItems.Select(li => new
    {
        li.LineItemId,
        li.ProductId,
        li.ProductName,
        li.Quantity,
        li.UnitPrice
    })
};

record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country);

record CreateProductRequest(string ProductName, string SKU, decimal UnitPrice, int StockQuantity = 0);
record AddStockRequest(int Quantity);
record CreateOrderRequest(Guid CustomerId);
record AddLineItemRequest(Guid ProductId, int Quantity);
record ActorDto(string Id, string[] Permissions);

public partial class Program
{
}

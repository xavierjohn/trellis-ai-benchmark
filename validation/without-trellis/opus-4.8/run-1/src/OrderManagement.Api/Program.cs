using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Api;
using OrderManagement.Api.Application.Abstractions;
using OrderManagement.Api.Application.Auth;
using OrderManagement.Api.Application.Customers;
using OrderManagement.Api.Application.Orders;
using OrderManagement.Api.Application.Products;
using OrderManagement.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("OrderManagement")
                       ?? "Data Source=OrderManagement.db";

builder.Services.AddDbContext<OrderManagementDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

builder.Services.AddScoped<IActorProvider, HttpActorProvider>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.UseExceptionHandler();

// Enforce api-version on /api/* routes (must run before endpoint execution).
app.UseMiddleware<ApiVersionMiddleware>();

app.MapOrderManagementEndpoints();

// Create the database schema on startup (EnsureCreated, no migrations).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

// Exposed for WebApplicationFactory in the integration tests.
public partial class Program { }

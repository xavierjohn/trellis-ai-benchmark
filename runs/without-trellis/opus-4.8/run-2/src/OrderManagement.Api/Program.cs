using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Endpoints;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Application.Abstractions;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=OrderManagement.db";

builder.Services.AddOrderManagement(options => options.UseSqlite(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActor>(sp =>
    ActorParser.Resolve(sp.GetRequiredService<IHttpContextAccessor>().HttpContext));

builder.Services.AddProblemDetails();

builder.Services
    .AddApiVersioning(options =>
    {
        options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Create the database schema on startup in development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(new DateOnly(2026, 11, 12)))
    .Build();

var api = app.MapGroup("/api").WithApiVersionSet(versionSet);
api.MapApiEndpoints();

app.Run();

public partial class Program { }

using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Endpoints;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Application;
using OrderManagement.Application.Abstractions;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrderManagement")
    ?? "Data Source=OrderManagement.db";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorProvider, HttpContextActorProvider>();

builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.UnsupportedApiVersionStatusCode = StatusCodes.Status400BadRequest;
});

var app = builder.Build();

app.UseStatusCodePages();

// Create the SQLite schema on startup (development convenience; no migrations).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var apiVersion = new ApiVersion(new DateOnly(2026, 11, 12));

ApiVersionSet versionSet = app.NewApiVersionSet()
    .HasApiVersion(apiVersion)
    .ReportApiVersions()
    .Build();

var api = app.MapGroup("/api")
    .WithApiVersionSet(versionSet)
    .HasApiVersion(apiVersion);

api.MapCustomerEndpoints();
api.MapProductEndpoints();
api.MapOrderEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;

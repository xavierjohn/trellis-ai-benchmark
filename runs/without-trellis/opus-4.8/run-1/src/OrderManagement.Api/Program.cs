using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Auth;
using OrderManagement.Api.Endpoints;
using OrderManagement.Application;
using OrderManagement.Application.Authorization;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=OrderManagement.db";

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IActorProvider, HttpActorProvider>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(new DateOnly(2026, 11, 12));
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var apiVersion = new ApiVersion(new DateOnly(2026, 11, 12));
var versionSet = app.NewApiVersionSet()
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

public partial class Program { }

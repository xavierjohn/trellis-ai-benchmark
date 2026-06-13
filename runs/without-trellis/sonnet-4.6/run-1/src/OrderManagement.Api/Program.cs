using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Endpoints;
using OrderManagement.Api.Middleware;
using OrderManagement.Application.Commands;
using OrderManagement.Application.Queries;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=OrderManagement.db";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(CreateCustomerCommand).Assembly,
        typeof(GetOrderByIdQuery).Assembly);
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
    db.Database.EnsureCreated();
}

app.UseOrderManagementExceptionHandler();
app.UseMiddleware<ActorMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();

public partial class Program { }

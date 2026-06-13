using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Auth;
using OrderManagement.Api.Domain;
using OrderManagement.Api.Endpoints;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=OrderManagement.db"));

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

// Initialize database
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Global exception handler → RFC 9457 Problem Details
app.UseExceptionHandler(exApp =>
{
    exApp.Run(async context =>
    {
        var exFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var ex = exFeature?.Error;

        context.Response.ContentType = "application/problem+json";

        var (status, title, detail) = ex switch
        {
            DomainValidationException ve => (422, "Unprocessable Content", ve.Message),
            NotFoundException nfe => (404, "Not Found", nfe.Message),
            ConflictException ce => (409, "Conflict", ce.Message),
            ForbiddenException fe => (403, "Forbidden", fe.Message),
            _ => (500, "Internal Server Error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{status - 399}",
            title,
            status,
            detail
        });
    });
});

// Actor extraction middleware
app.UseMiddleware<ActorMiddleware>();

// API version check middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        if (!context.Request.Query.ContainsKey("api-version"))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Bad Request",
                status = 400,
                detail = "The 'api-version' query parameter is required."
            });
            return;
        }
    }
    await next(context);
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// API endpoints
app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();

public partial class Program { }

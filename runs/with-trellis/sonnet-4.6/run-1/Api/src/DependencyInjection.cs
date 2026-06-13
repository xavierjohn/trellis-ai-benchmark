namespace OrderManagement.Api;

using System.Diagnostics;
using Asp.Versioning.Conventions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderManagement.Domain;
using Scalar.AspNetCore;
using Trellis.Asp;
using Trellis.Asp.Authorization;
using Trellis.Asp.Idempotency;
using Trellis.ServiceLevelIndicators;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IHostEnvironment environment)
    {
        services.ConfigureOpenTelemetry();
        services.ConfigureServiceLevelIndicators();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
                ctx.ProblemDetails.Extensions["traceId"] = traceId;
                if (ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
                    ctx.ProblemDetails.Detail = "An error occurred in our API. Please refer the trace id with our support team.";
                if (ctx.ProblemDetails.Status == StatusCodes.Status405MethodNotAllowed &&
                    ctx.HttpContext.Response.Headers.TryGetValue("Allow", out var allow))
                {
                    ctx.ProblemDetails.Extensions["allow"] = allow.ToString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            };
        });
        services.AddControllers();
        services.AddTrellisAspWithScalarValidation();
        services.AddResourceCollectionName<Customer>("customers");
        services.AddResourceCollectionName<Product>("products");
        services.AddResourceCollectionName<Order>("orders");
        services.AddResourceCollectionName<LineItem>("line-items");
        services.AddTrellisIdempotency();
        services.AddInMemoryIdempotencyStore();
        services.AddApiVersioning()
            .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
            .AddApiExplorer()
            .AddOpenApi(options => options.Document.AddScalarTransformers());
        services.AddHealthChecks();

        if (environment.IsDevelopment())
        {
            services.AddDevelopmentActorProvider(options =>
            {
                options.DefaultActorId = "admin";
                options.DefaultPermissions = new HashSet<string>
                {
                    Permissions.CustomersCreate,
                    Permissions.ProductsCreate,
                    Permissions.ProductsManageStock,
                    Permissions.OrdersCreate,
                    Permissions.OrdersSubmit,
                    Permissions.OrdersApprove,
                    Permissions.OrdersShip,
                    Permissions.OrdersDeliver,
                    Permissions.OrdersCancel,
                    Permissions.OrdersRead,
                    Permissions.OrdersReadAll,
                };
            });
        }
        else
        {
            throw new InvalidOperationException("Production IActorProvider not configured. Register a non-development actor provider.");
        }

        return services;
    }

    private static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
    {
        static void ConfigureResource(ResourceBuilder resourceBuilder) => resourceBuilder.AddService(
            serviceName: "OrderManagementService",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

        services.AddOpenTelemetry()
            .ConfigureResource(ConfigureResource)
            .WithMetrics(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddServiceLevelIndicatorInstrumentation();
                builder.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http");
                builder.AddOtlpExporter();
            })
            .WithTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddPrimitiveValueObjectInstrumentation();
                builder.AddSource("Trellis.Mediator");
                builder.AddOtlpExporter();
            });

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            var resourceBuilder = ResourceBuilder.CreateDefault();
            ConfigureResource(resourceBuilder);
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter();
        }));

        return services;
    }

    private static IServiceCollection ConfigureServiceLevelIndicators(this IServiceCollection services)
    {
        services.AddServiceLevelIndicator(options =>
        {
            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
        })
        .AddMvc()
        .AddApiVersion();

        return services;
    }
}

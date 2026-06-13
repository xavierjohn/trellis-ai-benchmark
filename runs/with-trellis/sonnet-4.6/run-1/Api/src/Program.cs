using Asp.Versioning;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api;
using OrderManagement.Application;
using Scalar.AspNetCore;
using Trellis.Asp;
using Trellis.Asp.Idempotency;
using Trellis.ServiceLevelIndicators;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation(builder.Environment)
    .AddApplication()
    .AddAntiCorruptionLayer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=OrderManagement.db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(options =>
    {
        var descriptions = app.DescribeApiVersions();
        for (var i = 0; i < descriptions.Count; i++)
        {
            var description = descriptions[i];
            var isDefault = i == descriptions.Count - 1;
            options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
        }
    });
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseTrellisIdempotency();
app.UseScalarValueValidation();
app.UseServiceLevelIndicator();
app.MapControllers();
app.MapHealthChecks("/health").WithMetadata(new ApiVersionNeutralAttribute());
app.Run();

/// <summary>Application entry-point marker for integration tests.</summary>
public partial class Program
{
}

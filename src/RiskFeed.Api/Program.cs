using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RiskFeed.Api.Endpoints;
using RiskFeed.Api.Middleware;
using RiskFeed.Core.Abstractions;
using RiskFeed.Core.Services;
using RiskFeed.Infrastructure;
using RiskFeed.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRiskEventValidator, RiskEventValidator>();
builder.Services.AddScoped<IRiskFeedService, RiskFeedService>();
builder.Services.AddRiskFeedInfrastructure();
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Risk Feed API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Risk Feed API v1");
});
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet(
        "/health/live",
        () => Results.Ok(new { status = "Healthy" }))
    .ExcludeFromDescription();
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready")
        });
app.MapHealthChecks("/health");
app.MapRiskFeedEndpoints();
app.MapFallbackToFile("index.html").ExcludeFromDescription();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    DatabaseInitializer initializer =
        scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

await app.RunAsync();

public partial class Program;

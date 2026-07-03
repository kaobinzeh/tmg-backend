using TMG.Consumer;
using TMG.Infrastructure.Authentication;
using TMG.Infrastructure.Caching;
using TMG.Infrastructure.Messaging;
using TMG.Infrastructure.Notifications;
using TMG.Infrastructure.Observability;
using TMG.Infrastructure.Persistence;
using TMG.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkerReadinessState>();
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddPostgresReadPersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddNotificationServices(builder.Configuration);
builder.Services.AddObjectStorage(builder.Configuration);
builder.Services.AddSubscribers(builder.Configuration);
builder.Services.AddCustomTelemetryContext();
builder.Services
    .AddHealthChecks()
    .AddCheck<ConsumerReadinessHealthCheck>(
        "consumer-readiness",
        tags: ["readiness"])
    .AddCheck<ConsumerLivenessHealthCheck>(
        "consumer-liveness",
        tags: ["liveness"]);
builder.Services.AddBackendTelemetry(builder.Configuration);

var app = builder.Build();

app.MapPrometheusScrapingEndpoint("/metrics");
app.MapHealthChecks("/health/readiness", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("readiness")
});

app.MapHealthChecks("/health/liveness", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("liveness")
});

app.MapGet("/", () => Results.Ok(new
{
    Service = "TMG.Consumer",
    Status = "Running"
}))
.ExcludeFromDescription();

await app.RunAsync();

public partial class Program;

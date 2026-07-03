using TMG.Application;
using TMG.Jobs.Authentication;
using TMG.Infrastructure.Messaging;
using TMG.Infrastructure.Observability;
using TMG.Infrastructure.Payments;
using TMG.Infrastructure.Persistence;
using TMG.Jobs.HealthChecks;
using TMG.Jobs.Infrastructure.BackgroundServices;
using TMG.Jobs.Observability;
using TMG.Jobs.OutboxProcessing;
using TMG.Jobs.Payments;
using TMG.Jobs.RentReminders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddBackgroundServiceReadinessTracking();
builder.Services.AddApplication();
builder.Services.AddCustomTelemetryContext();
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddPostgresReadPersistence(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddRabbitMqOutboxDispatching(builder.Configuration);
builder.Services.AddOutboxMessageProcessing(builder.Configuration);
builder.Services.AddIpAddressLocationEnrichment(builder.Configuration);
builder.Services.AddPaymentReconciliation(builder.Configuration);
builder.Services.AddRentReminders(builder.Configuration);
builder.Services.AddJobsHealthChecks();
builder.Services.AddJobsOpenTelemetry(builder.Configuration);

var app = builder.Build();

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
    Service = "TMG.Jobs",
    Status = "Running"
}))
.ExcludeFromDescription();

await app.RunAsync();

public partial class Program;

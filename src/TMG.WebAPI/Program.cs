using Asp.Versioning;
using TMG.Application;
using TMG.Infrastructure.Authentication;
using TMG.Infrastructure.Caching;
using TMG.Infrastructure.Messaging;
using TMG.Infrastructure.Notifications;
using TMG.Infrastructure.Observability;
using TMG.Infrastructure.Payments;
using TMG.Infrastructure.Persistence;
using TMG.Infrastructure.Storage;
using TMG.WebAPI.Features.Authentication.Registrations;
using TMG.WebAPI.Infrastructure.ApiDocumentation;
using TMG.WebAPI.Infrastructure;
using TMG.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<SignUpValidator>();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();
builder.Services.AddApplication();
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddPostgresReadPersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRequestRateLimiting(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddObjectStorage(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.AddNotificationWebhookServices(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddCustomTelemetryContext();
builder.Services.AddBackendTelemetry(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<CurrentActorMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.UseApiDocumentation();
app.MapPrometheusScrapingEndpoint("/metrics").DisableRateLimiting();
app.MapHealthChecks("/health").DisableRateLimiting();
app.MapControllers();
app.MapGet("/", () => TypedResults.Ok(new
{
    Service = "TMG.WebAPI",
    Status = "Healthy"
}))
.ExcludeFromDescription();

if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    await app.InitializeDatabaseAsync();
}

app.Run();

public partial class Program;

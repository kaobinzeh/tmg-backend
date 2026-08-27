using Microsoft.Extensions.Options;

namespace TMG.WebAPI.Infrastructure.Cors;

public static class WebApplicationExtensions
{
    public static WebApplication UseFrontendCors(this WebApplication app)
    {
        var allowedOrigins = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value.AllowedOrigins;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(WebApplicationExtensions));

        if (allowedOrigins.Length == 0)
        {
            // An empty allowlist is a valid deployment (server-to-server only), but it is also
            // what a missing environment variable looks like, so make it visible at startup.
            logger.LogWarning(
                "No CORS origins are configured; browser-direct calls will be rejected. Set '{Section}:{Key}' to allow a frontend origin.",
                CorsOptions.SectionName,
                nameof(CorsOptions.AllowedOrigins));
        }
        else
        {
            logger.LogInformation("CORS is allowing {OriginCount} frontend origin(s): {Origins}.", allowedOrigins.Length, allowedOrigins);
        }

        app.UseCors(CorsPolicyNames.Frontend);

        return app;
    }
}

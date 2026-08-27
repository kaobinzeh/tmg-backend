using Microsoft.Extensions.Options;

namespace TMG.WebAPI.Infrastructure.Proxy;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Rewrites the connection's remote IP and scheme from the forwarded headers. Must run
    /// before any middleware that reads either — rate limiting partitions on the client IP.
    /// </summary>
    public static WebApplication UseProxyForwardedHeaders(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ProxyForwardedHeadersOptions>>().Value;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(WebApplicationExtensions));

        if (!options.Enabled)
        {
            logger.LogInformation(
                "Forwarded headers are disabled; the connection's remote IP is used for rate limiting and session auditing.");
            return app;
        }

        app.UseForwardedHeaders();

        if (options.TrustAnyProxy)
        {
            logger.LogWarning(
                "Forwarded headers are accepted from any peer with a forward limit of {ForwardLimit}. " +
                "This is only safe while a reverse proxy is the sole route to this service.",
                options.ForwardLimit);
        }
        else
        {
            logger.LogInformation(
                "Forwarded headers are accepted from {ProxyCount} known proxies and {NetworkCount} known networks with a forward limit of {ForwardLimit}.",
                options.KnownProxies.Length,
                options.KnownNetworks.Length,
                options.ForwardLimit);
        }

        return app;
    }
}

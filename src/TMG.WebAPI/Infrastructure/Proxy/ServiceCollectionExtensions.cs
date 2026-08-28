using System.Net;

namespace TMG.WebAPI.Infrastructure.Proxy;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProxyForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ProxyForwardedHeadersOptions.SectionName);
        var options = section.Get<ProxyForwardedHeadersOptions>() ?? new ProxyForwardedHeadersOptions();

        Validate(options);
        services.Configure<ProxyForwardedHeadersOptions>(section);

        if (!options.Enabled)
        {
            return services;
        }

        var knownProxies = options.KnownProxies.Select(ParseProxy).ToArray();
        var knownNetworks = options.KnownNetworks.Select(ParseNetwork).ToArray();

        services.Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            forwardedHeadersOptions.ForwardedHeaders =
                Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            forwardedHeadersOptions.ForwardLimit = options.ForwardLimit;

            // The framework defaults trust loopback only, which never matches an ingress
            // running in a different container or on a different host.
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownIPNetworks.Clear();

            if (options.TrustAnyProxy)
            {
                return;
            }

            foreach (var proxy in knownProxies)
            {
                forwardedHeadersOptions.KnownProxies.Add(proxy);
            }

            foreach (var network in knownNetworks)
            {
                forwardedHeadersOptions.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }

    private static void Validate(ProxyForwardedHeadersOptions options)
    {
        if (options.ForwardLimit <= 0)
        {
            throw new InvalidOperationException(
                $"'{ProxyForwardedHeadersOptions.SectionName}:{nameof(ProxyForwardedHeadersOptions.ForwardLimit)}' must be greater than zero.");
        }

        if (!options.Enabled)
        {
            return;
        }

        // Without a trusted peer the middleware silently ignores every forwarded header,
        // which is the failure this configuration exists to prevent.
        if (!options.TrustAnyProxy && options.KnownProxies.Length == 0 && options.KnownNetworks.Length == 0)
        {
            throw new InvalidOperationException(
                $"'{ProxyForwardedHeadersOptions.SectionName}' is enabled but no proxy is trusted. Set " +
                $"'{nameof(ProxyForwardedHeadersOptions.TrustAnyProxy)}' or supply " +
                $"'{nameof(ProxyForwardedHeadersOptions.KnownProxies)}'/'{nameof(ProxyForwardedHeadersOptions.KnownNetworks)}'.");
        }
    }

    private static IPAddress ParseProxy(string value) =>
        IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException(
                $"'{ProxyForwardedHeadersOptions.SectionName}:{nameof(ProxyForwardedHeadersOptions.KnownProxies)}' contains an invalid IP address '{value}'.");

    private static IPNetwork ParseNetwork(string value) =>
        IPNetwork.TryParse(value, out var network)
            ? network
            : throw new InvalidOperationException(
                $"'{ProxyForwardedHeadersOptions.SectionName}:{nameof(ProxyForwardedHeadersOptions.KnownNetworks)}' contains an invalid CIDR network '{value}'.");
}

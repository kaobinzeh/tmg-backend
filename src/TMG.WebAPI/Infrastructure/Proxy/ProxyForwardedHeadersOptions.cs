namespace TMG.WebAPI.Infrastructure.Proxy;

/// <summary>
/// Controls whether <c>X-Forwarded-For</c> / <c>X-Forwarded-Proto</c> are honoured.
/// The client IP partitions every rate-limit policy and is recorded against sessions,
/// so behind a reverse proxy these headers must be trusted or every caller collapses
/// into the proxy's single partition.
/// </summary>
public sealed class ProxyForwardedHeadersOptions
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Enable only when a reverse proxy fronts the service. Leaving this on while the
    /// service is directly reachable lets callers spoof their own IP.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Number of proxy hops to unwind, counted from the right of the header. Matches the
    /// number of proxies in front of the service; 1 for a single ingress.
    /// </summary>
    public int ForwardLimit { get; init; } = 1;

    /// <summary>
    /// Accept forwarded headers from any peer. Correct when a single ingress fronts the
    /// service and the container is not otherwise reachable, which is the usual shape on
    /// managed container hosts where the ingress address rotates.
    /// </summary>
    public bool TrustAnyProxy { get; init; }

    /// <summary>Individual proxy addresses to trust, e.g. <c>10.0.1.7</c>.</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>Proxy networks to trust in CIDR form, e.g. <c>10.0.0.0/8</c>.</summary>
    public string[] KnownNetworks { get; init; } = [];
}

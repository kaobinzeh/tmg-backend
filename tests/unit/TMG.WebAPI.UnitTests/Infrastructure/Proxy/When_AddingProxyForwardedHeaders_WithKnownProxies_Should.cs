using System.Net;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Infrastructure.Proxy;

public sealed class When_AddingProxyForwardedHeaders_WithKnownProxies_Should
{
    [Fact]
    public void TrustOnlyTheConfiguredProxiesAndNetworks()
    {
        const string knownProxy = "10.0.1.7";
        const string knownNetwork = "10.2.0.0/16";

        var options = ProxyForwardedHeadersTestContext.ResolveForwardedHeadersOptions(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:ForwardLimit"] = "2",
            ["ForwardedHeaders:KnownProxies:0"] = knownProxy,
            ["ForwardedHeaders:KnownNetworks:0"] = knownNetwork
        });

        options.ForwardLimit.ShouldBe(2);
        options.KnownProxies.ShouldHaveSingleItem().ShouldBe(IPAddress.Parse(knownProxy));
        options.KnownIPNetworks.ShouldHaveSingleItem().ShouldBe(IPNetwork.Parse(knownNetwork));
    }
}

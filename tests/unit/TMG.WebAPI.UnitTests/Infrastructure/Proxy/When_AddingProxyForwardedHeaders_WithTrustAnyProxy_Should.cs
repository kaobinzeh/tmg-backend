using Microsoft.AspNetCore.HttpOverrides;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Infrastructure.Proxy;

public sealed class When_AddingProxyForwardedHeaders_WithTrustAnyProxy_Should
{
    [Fact]
    public void AcceptForwardedHeadersFromAnyPeer()
    {
        var options = ProxyForwardedHeadersTestContext.ResolveForwardedHeadersOptions(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:TrustAnyProxy"] = "true",
            ["ForwardedHeaders:ForwardLimit"] = "1"
        });

        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        options.ForwardLimit.ShouldBe(1);

        // The loopback defaults would reject an ingress running on another host.
        options.KnownProxies.ShouldBeEmpty();
        options.KnownIPNetworks.ShouldBeEmpty();
    }
}

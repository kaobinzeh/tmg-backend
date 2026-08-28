using Shouldly;

namespace TMG.WebAPI.UnitTests.Infrastructure.Proxy;

public sealed class When_AddingProxyForwardedHeaders_WithInvalidKnownNetwork_Should
{
    [Fact]
    public void ThrowForTheMalformedNetwork()
    {
        const string malformedNetwork = "10.2.0.0/not-a-prefix";

        var register = () => ProxyForwardedHeadersTestContext.Register(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:KnownNetworks:0"] = malformedNetwork
        });

        register.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain(malformedNetwork);
    }
}

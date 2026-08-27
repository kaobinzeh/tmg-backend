using Shouldly;

namespace TMG.WebAPI.UnitTests.Infrastructure.Proxy;

public sealed class When_AddingProxyForwardedHeaders_WithNoTrustedProxy_Should
{
    [Fact]
    public void ThrowRatherThanSilentlyIgnoreForwardedHeaders()
    {
        var register = () => ProxyForwardedHeadersTestContext.Register(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "true"
        });

        register.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("no proxy is trusted");
    }
}

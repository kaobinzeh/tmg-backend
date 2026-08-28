using Microsoft.AspNetCore.HttpOverrides;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Infrastructure.Proxy;

public sealed class When_AddingProxyForwardedHeaders_WithSectionDisabled_Should
{
    [Fact]
    public void LeaveForwardedHeaderProcessingUnconfigured()
    {
        var options = ProxyForwardedHeadersTestContext.ResolveForwardedHeadersOptions(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "false"
        });

        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.None);
    }
}

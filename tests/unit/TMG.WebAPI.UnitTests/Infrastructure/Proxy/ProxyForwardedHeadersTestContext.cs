using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TMG.WebAPI.Infrastructure.Proxy;

namespace TMG.WebAPI.UnitTests.Infrastructure.Proxy;

internal static class ProxyForwardedHeadersTestContext
{
    public static IServiceCollection Register(IReadOnlyDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection().AddProxyForwardedHeaders(configuration);
    }

    public static ForwardedHeadersOptions ResolveForwardedHeadersOptions(IReadOnlyDictionary<string, string?> settings) =>
        Register(settings)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;
}

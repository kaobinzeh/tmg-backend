using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using TMG.Application;
using TMG.Application.Authentication;

namespace TMG.Application.UnitTests.Authentication;

public sealed class When_AddingApplication_WithDefaultClientId_Should
{
    [Fact]
    public void BindTheConfiguredClientId()
    {
        var defaultClientId = Guid.CreateVersion7();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Clients:Onboarding:DefaultClientId"] = defaultClientId.ToString()
            })
            .Build();

        var options = new ServiceCollection()
            .AddApplication(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ClientOnboardingOptions>>()
            .Value;

        options.DefaultClientId.ShouldBe(defaultClientId);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TMG.Application;

namespace TMG.Application.UnitTests.Authentication;

public sealed class When_AddingApplication_WithoutDefaultClientId_Should
{
    [Fact]
    public void ThrowSoTheDeploymentFailsBeforeSignUpDoes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Clients:Onboarding:DefaultClientId"] = string.Empty
            })
            .Build();

        var addApplication = () => new ServiceCollection().AddApplication(configuration);

        addApplication.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("Clients:Onboarding:DefaultClientId");
    }
}

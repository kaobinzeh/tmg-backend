using TMG.WebAPI.IntegrationTests.Properties;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Stakeholders.Me;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingMyProfile_WithAuthenticatedManager_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private sealed record MyProfileView(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string? AvatarUrl,
        string StakeholderTypeKey,
        Guid ClientId);

    private HttpResponseMessage? _response;

    [Fact]
    public async Task ReturnTheManagerProfile()
    {
        MyProfileView? payload = null;

        await WhenGettingMyProfile();
        ThenTheManagerProfileIsReturned();

        async Task WhenGettingMyProfile()
        {
            _response = await Client.GetAsync(EndpointUrl.Stakeholders.MeV1);
            payload = await _response.Content.ReadFromJsonAsync<MyProfileView>();
        }

        void ThenTheManagerProfileIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            payload.ShouldNotBeNull();
            payload.Id.ShouldBe(StakeholderId);
            payload.Email.ShouldBe(Email, StringCompareShould.IgnoreCase);
            payload.StakeholderTypeKey.ShouldBe("manager");
            payload.ClientId.ShouldBe(ClientId);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

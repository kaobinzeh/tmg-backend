using TMG.Application.Properties.Features.ListProperties;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.ListProperties;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingProperties_WithAuthenticatedManager_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task ReturnTheClientProperties()
    {
        var propertyId = await SeedPropertyAsync("Maitama Heights", "5 Gana Street");
        IReadOnlyList<PropertyListItem>? payload = null;

        await WhenListingProperties();
        await ThenThePropertyIsReturned();

        async Task WhenListingProperties()
        {
            _response = await Client.GetAsync(EndpointUrl.Properties.V1);
        }

        async Task ThenThePropertyIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            payload = await _response.Content.ReadFromJsonAsync<IReadOnlyList<PropertyListItem>>();
            payload.ShouldNotBeNull();
            payload.ShouldContain(item => item.Id == propertyId && item.Name == "Maitama Heights");
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

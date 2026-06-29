using TMG.Application.Properties.Features.ListUnits;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.ListUnits;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingUnits_WithExistingProperty_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task ReturnThePropertyUnits()
    {
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId, "Flat 7");
        IReadOnlyList<UnitListItem>? payload = null;

        await WhenListingUnits();
        await ThenTheUnitIsReturned();

        async Task WhenListingUnits()
        {
            _response = await Client.GetAsync(EndpointUrl.Properties.UnitsV1(propertyId));
        }

        async Task ThenTheUnitIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            payload = await _response.Content.ReadFromJsonAsync<IReadOnlyList<UnitListItem>>();
            payload.ShouldNotBeNull();
            payload.ShouldContain(item => item.Id == unitId && item.Label == "Flat 7");
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

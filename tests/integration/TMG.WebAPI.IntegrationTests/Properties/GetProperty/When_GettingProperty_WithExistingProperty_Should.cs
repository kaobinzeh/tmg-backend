using TMG.Application.Properties.Features.GetProperty;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.GetProperty;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingProperty_WithExistingProperty_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task ReturnThePropertyDetail()
    {
        var propertyId = await SeedPropertyAsync("Maitama Heights", "5 Gana Street");
        PropertyDetail? payload = null;

        await WhenGettingTheProperty();
        await ThenThePropertyDetailIsReturned();

        async Task WhenGettingTheProperty()
        {
            _response = await Client.GetAsync(EndpointUrl.Properties.ByIdV1(propertyId));
        }

        async Task ThenThePropertyDetailIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            payload = await _response.Content.ReadFromJsonAsync<PropertyDetail>();
            payload.ShouldNotBeNull();
            payload.Id.ShouldBe(propertyId);
            payload.Name.ShouldBe("Maitama Heights");
            payload.Address.ShouldBe("5 Gana Street");
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

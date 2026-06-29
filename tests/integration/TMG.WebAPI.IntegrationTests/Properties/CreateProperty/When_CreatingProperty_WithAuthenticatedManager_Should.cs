using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.WebAPI.Features.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.CreateProperty;

[Collection(nameof(ContainersCollection))]
public sealed class When_CreatingProperty_WithAuthenticatedManager_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task PersistTheProperty()
    {
        CreatePropertyResponse? payload = null;

        await WhenCreatingProperty();
        await ThenThePropertyIsPersisted();

        async Task WhenCreatingProperty()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Properties.V1,
                new CreatePropertyRequest("Lekki Court", "12 Admiralty Way", "Block of flats"));
        }

        async Task ThenThePropertyIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Created);

            payload = await _response.Content.ReadFromJsonAsync<CreatePropertyResponse>();
            payload.ShouldNotBeNull();

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Property>>();
            var property = await repository.GetByIdAsync(payload.Id);

            property.ShouldNotBeNull();
            property.ClientId.ShouldBe(ClientId);
            property.OwnerStakeholderId.ShouldBe(StakeholderId);
            property.Name.ShouldBe("Lekki Court");
            property.Address.ShouldBe("12 Admiralty Way");
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

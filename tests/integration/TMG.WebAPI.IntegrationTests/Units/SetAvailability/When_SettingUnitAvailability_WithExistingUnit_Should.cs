using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.WebAPI.Features.Properties;
using TMG.WebAPI.IntegrationTests.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Units.SetAvailability;

[Collection(nameof(ContainersCollection))]
public sealed class When_SettingUnitAvailability_WithExistingUnit_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task PersistTheNewStatus()
    {
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId, "Flat 4");

        await WhenSettingAvailability();
        await ThenTheStatusIsUpdated();

        async Task WhenSettingAvailability()
        {
            _response = await Client.PutAsJsonAsync(
                EndpointUrl.Units.AvailabilityV1(unitId),
                new SetUnitAvailabilityRequest(UnitStatus.Occupied));
        }

        async Task ThenTheStatusIsUpdated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Unit>>();
            var unit = await repository.GetByIdAsync(unitId);

            unit.ShouldNotBeNull();
            unit.Status.ShouldBe(UnitStatus.Occupied);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

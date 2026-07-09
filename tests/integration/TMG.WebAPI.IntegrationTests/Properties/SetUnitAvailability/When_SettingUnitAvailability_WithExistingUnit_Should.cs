using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.WebAPI.Features.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.SetUnitAvailability;

[Collection(nameof(ContainersCollection))]
public sealed class When_SettingUnitAvailability_WithExistingUnit_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task PersistTheNewStatus()
    {
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId);

        await WhenMarkingTheUnitUnavailable();
        await ThenTheStatusIsPersisted();

        async Task WhenMarkingTheUnitUnavailable()
        {
            _response = await Client.PutAsJsonAsync(
                EndpointUrl.Units.AvailabilityV1(unitId),
                new SetUnitAvailabilityRequest(UnitStatus.Unavailable));
        }

        async Task ThenTheStatusIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var unitRepository = scope.ServiceProvider.GetRequiredService<IRepository<Unit>>();
            var unit = await unitRepository.GetByIdAsync(unitId);
            unit.ShouldNotBeNull();
            unit.Status.ShouldBe(UnitStatus.Unavailable);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

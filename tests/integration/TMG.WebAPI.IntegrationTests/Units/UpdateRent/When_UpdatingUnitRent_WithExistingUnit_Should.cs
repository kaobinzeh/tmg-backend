using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.WebAPI.Features.Properties;
using TMG.WebAPI.IntegrationTests.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Units.UpdateRent;

[Collection(nameof(ContainersCollection))]
public sealed class When_UpdatingUnitRent_WithExistingUnit_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task PersistTheNewRent()
    {
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId, "Flat 3", 1_000_000m);

        await WhenUpdatingRent();
        await ThenTheRentIsUpdated();

        async Task WhenUpdatingRent()
        {
            _response = await Client.PutAsJsonAsync(
                EndpointUrl.Units.RentV1(unitId),
                new UpdateUnitRentRequest(1_350_000m));
        }

        async Task ThenTheRentIsUpdated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Unit>>();
            var unit = await repository.GetByIdAsync(unitId);

            unit.ShouldNotBeNull();
            unit.RentAmount.ShouldBe(1_350_000m);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

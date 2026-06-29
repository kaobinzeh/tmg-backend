using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.WebAPI.Features.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties.AddUnit;

[Collection(nameof(ContainersCollection))]
public sealed class When_AddingUnit_WithExistingProperty_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task PersistTheUnit()
    {
        var propertyId = await SeedPropertyAsync();
        var currencyId = Guid.CreateVersion7();
        AddUnitResponse? payload = null;

        await WhenAddingUnit();
        await ThenTheUnitIsPersisted();

        async Task WhenAddingUnit()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Properties.UnitsV1(propertyId),
                new AddUnitRequest("Flat 2", "Three bedroom", 4, 2_000_000m, currencyId));
        }

        async Task ThenTheUnitIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Created);

            payload = await _response.Content.ReadFromJsonAsync<AddUnitResponse>();
            payload.ShouldNotBeNull();

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Unit>>();
            var unit = await repository.GetByIdAsync(payload.Id);

            unit.ShouldNotBeNull();
            unit.PropertyId.ShouldBe(propertyId);
            unit.ClientId.ShouldBe(ClientId);
            unit.Label.ShouldBe("Flat 2");
            unit.NumberOfRooms.ShouldBe(4);
            unit.RentAmount.ShouldBe(2_000_000m);
            unit.Status.ShouldBe(UnitStatus.Available);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

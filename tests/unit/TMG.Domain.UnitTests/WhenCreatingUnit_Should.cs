using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenCreatingUnit_Should
{
    [Fact]
    public void StartAvailableWithProvidedDetails()
    {
        var propertyId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();

        var unit = Unit.Create(propertyId, clientId, "  Flat 1  ", "  Two bedroom  ", 3, 1_500_000m, currencyId);

        unit.PropertyId.ShouldBe(propertyId);
        unit.ClientId.ShouldBe(clientId);
        unit.Label.ShouldBe("Flat 1");
        unit.Description.ShouldBe("Two bedroom");
        unit.NumberOfRooms.ShouldBe(3);
        unit.RentAmount.ShouldBe(1_500_000m);
        unit.CurrencyId.ShouldBe(currencyId);
        unit.Status.ShouldBe(UnitStatus.Available);
    }
}

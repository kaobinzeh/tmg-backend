using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests;

public sealed class WhenUpdatingUnitRent_Should
{
    [Fact]
    public void ReplaceTheRentAmount()
    {
        var unit = Unit.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Flat 1", null, 2, 1_000_000m, Guid.CreateVersion7());

        unit.UpdateRent(1_250_000m);

        unit.RentAmount.ShouldBe(1_250_000m);
    }
}

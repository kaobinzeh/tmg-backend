using TMG.Application.Properties.Features.ListUnits;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Properties.ListUnits;

public sealed class When_ListingUnits_WithCurrentTenancies_Should
{
    [Fact]
    public async Task PopulateTenancyFieldsOnlyForOccupiedUnits()
    {
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var unitRepository = Substitute.For<IRepository<Unit>>();
        var tenancyReadModelRepository = Substitute.For<ITenancyReadModelRepository>();
        var handler = new ListUnitsHandler(propertyRepository, unitRepository, tenancyReadModelRepository);

        var clientId = Guid.CreateVersion7();
        var propertyId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        var occupiedUnit = Unit.Create(propertyId, clientId, "Flat 1", "Two bedroom", 2, 1_500_000m, currencyId);
        var vacantUnit = Unit.Create(propertyId, clientId, "Flat 2", null, 3, 2_000_000m, currencyId);
        var tenancyId = Guid.CreateVersion7();

        propertyRepository.AnyAsync(Arg.Any<ISpecification<Property>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        unitRepository.ListAsync(Arg.Any<ISpecification<Unit>>(), Arg.Any<CancellationToken>())
            .Returns([occupiedUnit, vacantUnit]);
        tenancyReadModelRepository.ListCurrentByPropertyAsync(clientId, propertyId, Arg.Any<CancellationToken>())
            .Returns([
                new UnitTenancyReadModel(occupiedUnit.Id, tenancyId, TenancyStatus.Active, "Ada Lovelace", "ada@example.com")
            ]);

        var result = await handler.HandleAsync(
            new ListUnitsCommand(propertyId, new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListUnitsStatus.Success);
        result.Units.Count.ShouldBe(2);

        var occupied = result.Units.Single(unit => unit.Id == occupiedUnit.Id);
        occupied.Label.ShouldBe("Flat 1");
        occupied.CurrentTenancyId.ShouldBe(tenancyId);
        occupied.CurrentTenancyStatus.ShouldBe("Active");
        occupied.TenantName.ShouldBe("Ada Lovelace");
        occupied.TenantEmail.ShouldBe("ada@example.com");

        var vacant = result.Units.Single(unit => unit.Id == vacantUnit.Id);
        vacant.Label.ShouldBe("Flat 2");
        vacant.CurrentTenancyId.ShouldBeNull();
        vacant.CurrentTenancyStatus.ShouldBeNull();
        vacant.TenantName.ShouldBeNull();
        vacant.TenantEmail.ShouldBeNull();
    }
}

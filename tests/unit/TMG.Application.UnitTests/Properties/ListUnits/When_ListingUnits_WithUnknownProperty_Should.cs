using TMG.Application.Properties.Features.ListUnits;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Properties.ListUnits;

public sealed class When_ListingUnits_WithUnknownProperty_Should
{
    [Fact]
    public async Task ReturnPropertyNotFound()
    {
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var unitRepository = Substitute.For<IRepository<Unit>>();
        var tenancyReadModelRepository = Substitute.For<ITenancyReadModelRepository>();
        var handler = new ListUnitsHandler(propertyRepository, unitRepository, tenancyReadModelRepository);

        propertyRepository.AnyAsync(Arg.Any<ISpecification<Property>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.HandleAsync(
            new ListUnitsCommand(
                Guid.CreateVersion7(),
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListUnitsStatus.PropertyNotFound);
        result.Units.ShouldBeEmpty();
        await unitRepository.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
        await tenancyReadModelRepository.DidNotReceiveWithAnyArgs()
            .ListCurrentByPropertyAsync(default, default, default);
    }
}

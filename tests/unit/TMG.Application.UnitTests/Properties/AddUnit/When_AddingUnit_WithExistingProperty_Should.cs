using TMG.Application.Properties.Features.AddUnit;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Properties.AddUnit;

public sealed class When_AddingUnit_WithExistingProperty_Should
{
    [Fact]
    public async Task PersistUnitAndReturnId()
    {
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var unitRepository = Substitute.For<IRepository<Unit>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddUnitHandler(propertyRepository, unitRepository, unitOfWork);

        var clientId = Guid.CreateVersion7();
        var propertyId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        var actorContext = new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow");

        propertyRepository.AnyAsync(Arg.Any<ISpecification<Property>>(), Arg.Any<CancellationToken>()).Returns(true);
        unitRepository.AnyAsync(Arg.Any<ISpecification<Unit>>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.HandleAsync(
            new AddUnitCommand(propertyId, "Flat 1", "Two bedroom", 3, 1_500_000m, currencyId, actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(AddUnitStatus.Success);
        result.UnitId.ShouldNotBeNull();
        await unitRepository.Received(1).AddAsync(
            Arg.Is<Unit>(unit => unit.PropertyId == propertyId && unit.ClientId == clientId && unit.NumberOfRooms == 3),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

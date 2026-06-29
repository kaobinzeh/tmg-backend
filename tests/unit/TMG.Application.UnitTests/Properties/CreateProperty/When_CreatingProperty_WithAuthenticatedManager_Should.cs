using TMG.Application.Properties.Features.CreateProperty;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Properties.CreateProperty;

public sealed class When_CreatingProperty_WithAuthenticatedManager_Should
{
    [Fact]
    public async Task PersistPropertyAndReturnId()
    {
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreatePropertyHandler(propertyRepository, unitOfWork);

        var clientId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var actorContext = new ActorContext(stakeholderId, clientId, "corr", "flow");

        var result = await handler.HandleAsync(
            new CreatePropertyCommand("Lekki Court", "12 Admiralty Way", "Block of flats", actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(CreatePropertyStatus.Success);
        result.PropertyId.ShouldNotBeNull();
        await propertyRepository.Received(1).AddAsync(
            Arg.Is<Property>(property => property.ClientId == clientId && property.OwnerStakeholderId == stakeholderId),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

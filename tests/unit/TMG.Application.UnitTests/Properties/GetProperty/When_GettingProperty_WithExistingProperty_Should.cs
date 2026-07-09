using TMG.Application.Properties.Features.GetProperty;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Properties.GetProperty;

public sealed class When_GettingProperty_WithExistingProperty_Should
{
    [Fact]
    public async Task ReturnThePropertyDetail()
    {
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var handler = new GetPropertyHandler(propertyRepository);
        var clientId = Guid.CreateVersion7();
        var property = Property.Create(clientId, Guid.CreateVersion7(), "Lekki Court", "12 Admiralty Way", "Serviced flats");

        propertyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Property>>(), Arg.Any<CancellationToken>())
            .Returns(property);

        var result = await handler.HandleAsync(
            new GetPropertyCommand(property.Id, new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetPropertyStatus.Success);
        result.Property.ShouldNotBeNull();
        result.Property.Id.ShouldBe(property.Id);
        result.Property.Name.ShouldBe("Lekki Court");
        result.Property.Address.ShouldBe("12 Admiralty Way");
        result.Property.Description.ShouldBe("Serviced flats");
    }
}

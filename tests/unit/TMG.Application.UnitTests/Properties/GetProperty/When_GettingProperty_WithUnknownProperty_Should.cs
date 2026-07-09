using TMG.Application.Properties.Features.GetProperty;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Properties.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Properties.GetProperty;

public sealed class When_GettingProperty_WithUnknownProperty_Should
{
    [Fact]
    public async Task ReturnPropertyNotFound()
    {
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var handler = new GetPropertyHandler(propertyRepository);

        propertyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Property>>(), Arg.Any<CancellationToken>())
            .Returns((Property?)null);

        var result = await handler.HandleAsync(
            new GetPropertyCommand(
                Guid.CreateVersion7(),
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetPropertyStatus.PropertyNotFound);
        result.Property.ShouldBeNull();
    }
}

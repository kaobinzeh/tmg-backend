using TMG.Application.Providers.Features.ActivateProvider;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.Providers.ActivateProvider;

public sealed class When_ActivatingProvider_WithUnknownProviderKey_Should
{
    [Fact]
    public async Task ReturnProviderNotFound()
    {
        var providerRepository = Substitute.For<IRepository<Provider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        providerRepository.ListAsync(Arg.Any<ISpecification<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([
                Provider.Create(ProviderType.Email, "Primary", "primary", true)
            ]);

        var sut = new ActivateProviderHandler(providerRepository, unitOfWork);

        var result = await sut.HandleAsync(
            new ActivateProviderCommand(ProviderType.Email, "missing"),
            CancellationToken.None);

        result.Status.ShouldBe(ActivateProviderStatus.ProviderNotFound);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}



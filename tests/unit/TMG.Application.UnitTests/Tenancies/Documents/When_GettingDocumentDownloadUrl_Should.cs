using TMG.Application.Tenancies;
using TMG.Application.Tenancies.Features.GetTenancyDocumentDownloadUrl;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.Documents;

public sealed class When_GettingDocumentDownloadUrl_Should
{
    private static readonly Guid ClientId = Guid.CreateVersion7();

    [Fact]
    public async Task ReturnASignedUrlForTheTenantOwner()
    {
        var tenantStakeholderId = Guid.CreateVersion7();
        var tenancy = Tenancy.Invite(ClientId, Guid.CreateVersion7(), Guid.CreateVersion7(), tenantStakeholderId, "tenant@example.com");
        var document = TenancyDocument.Create(ClientId, tenancy.Id, TenancyDocumentType.Agreement, "https://storage/private/agreement.html", "text/html", null);

        var documentRepository = Substitute.For<IRepository<TenancyDocument>>();
        documentRepository.FirstOrDefaultAsync(Arg.Any<TenancyDocumentByIdForClientSpecification>(), Arg.Any<CancellationToken>()).Returns(document);
        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        tenancyRepository.FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>()).Returns(tenancy);
        var objectStorage = Substitute.For<IObjectStorageService>();
        objectStorage.GetSignedDownloadUrlAsync(document.StorageKey, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns("https://storage/private/agreement.html?signed=token");
        var guard = new TenancyDocumentAccessGuard(Substitute.For<IRepository<Stakeholder>>(), Substitute.For<IRepository<StakeholderType>>());

        var handler = new GetTenancyDocumentDownloadUrlHandler(documentRepository, tenancyRepository, guard, objectStorage, TimeProvider.System);

        var result = await handler.HandleAsync(
            new GetTenancyDocumentDownloadUrlCommand(document.Id, new ActorContext(tenantStakeholderId, ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetTenancyDocumentDownloadUrlStatus.Success);
        result.Url.ShouldBe("https://storage/private/agreement.html?signed=token");
        result.ExpiresAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task ForbidAStakeholderWhoDoesNotOwnTheTenancyAndIsNotAManager()
    {
        var tenancy = Tenancy.Invite(ClientId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");
        var document = TenancyDocument.Create(ClientId, tenancy.Id, TenancyDocumentType.Agreement, "https://storage/private/agreement.html", "text/html", null);

        var documentRepository = Substitute.For<IRepository<TenancyDocument>>();
        documentRepository.FirstOrDefaultAsync(Arg.Any<TenancyDocumentByIdForClientSpecification>(), Arg.Any<CancellationToken>()).Returns(document);
        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        tenancyRepository.FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>()).Returns(tenancy);

        var strangerType = StakeholderType.Create(ClientId, "Tenant", "tenant");
        var managerType = StakeholderType.Create(ClientId, "Manager", "manager");
        var stranger = Stakeholder.Create(Guid.CreateVersion7(), ClientId, Guid.CreateVersion7(), strangerType.Id, "Sam", "Stranger");
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        stakeholderRepository.GetByIdAsync(stranger.Id, Arg.Any<CancellationToken>()).Returns(stranger);
        var stakeholderTypeRepository = Substitute.For<IRepository<StakeholderType>>();
        stakeholderTypeRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<StakeholderType>>(), Arg.Any<CancellationToken>()).Returns(managerType);

        var objectStorage = Substitute.For<IObjectStorageService>();
        var guard = new TenancyDocumentAccessGuard(stakeholderRepository, stakeholderTypeRepository);
        var handler = new GetTenancyDocumentDownloadUrlHandler(documentRepository, tenancyRepository, guard, objectStorage, TimeProvider.System);

        var result = await handler.HandleAsync(
            new GetTenancyDocumentDownloadUrlCommand(document.Id, new ActorContext(stranger.Id, ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetTenancyDocumentDownloadUrlStatus.Forbidden);
        await objectStorage.DidNotReceive().GetSignedDownloadUrlAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}

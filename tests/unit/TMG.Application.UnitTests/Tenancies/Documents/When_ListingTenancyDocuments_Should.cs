using TMG.Application.Tenancies;
using TMG.Application.Tenancies.Features.ListTenancyDocuments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.Documents;

public sealed class When_ListingTenancyDocuments_Should
{
    private static readonly Guid ClientId = Guid.CreateVersion7();

    private static (ListTenancyDocumentsHandler Handler, Tenancy Tenancy) Build(
        IRepository<Stakeholder>? stakeholderRepository = null,
        IRepository<StakeholderType>? stakeholderTypeRepository = null)
    {
        var tenantStakeholderId = Guid.CreateVersion7();
        var tenancy = Tenancy.Invite(ClientId, Guid.CreateVersion7(), Guid.CreateVersion7(), tenantStakeholderId, "tenant@example.com");

        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        tenancyRepository.FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>()).Returns(tenancy);

        var documentRepository = Substitute.For<IRepository<TenancyDocument>>();
        documentRepository.ListAsync(Arg.Any<TenancyDocumentsByTenancySpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<TenancyDocument>
            {
                TenancyDocument.Create(ClientId, tenancy.Id, TenancyDocumentType.Receipt, "https://storage/receipt.html", "text/html", null)
            });

        var guard = new TenancyDocumentAccessGuard(
            stakeholderRepository ?? Substitute.For<IRepository<Stakeholder>>(),
            stakeholderTypeRepository ?? Substitute.For<IRepository<StakeholderType>>());

        return (new ListTenancyDocumentsHandler(tenancyRepository, documentRepository, guard), tenancy);
    }

    [Fact]
    public async Task ReturnDocumentsForTheTenantOwner()
    {
        var (handler, tenancy) = Build();

        var result = await handler.HandleAsync(
            new ListTenancyDocumentsCommand(tenancy.Id, new ActorContext(tenancy.TenantStakeholderId, ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListTenancyDocumentsStatus.Success);
        result.Documents.ShouldNotBeNull();
        result.Documents.Count.ShouldBe(1);
        result.Documents[0].DocumentType.ShouldBe(TenancyDocumentType.Receipt);
    }

    [Fact]
    public async Task ReturnDocumentsForAManagerOfTheClient()
    {
        var managerType = StakeholderType.Create(ClientId, "Manager", "manager");
        var manager = Stakeholder.Create(Guid.CreateVersion7(), ClientId, Guid.CreateVersion7(), managerType.Id, "Mia", "Manager");

        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        stakeholderRepository.GetByIdAsync(manager.Id, Arg.Any<CancellationToken>()).Returns(manager);
        var stakeholderTypeRepository = Substitute.For<IRepository<StakeholderType>>();
        stakeholderTypeRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<StakeholderType>>(), Arg.Any<CancellationToken>()).Returns(managerType);

        var (handler, tenancy) = Build(stakeholderRepository, stakeholderTypeRepository);

        var result = await handler.HandleAsync(
            new ListTenancyDocumentsCommand(tenancy.Id, new ActorContext(manager.Id, ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListTenancyDocumentsStatus.Success);
    }

    [Fact]
    public async Task ForbidAStakeholderWhoIsNeitherTheTenantNorAManager()
    {
        var otherType = StakeholderType.Create(ClientId, "Tenant", "tenant");
        var manangerType = StakeholderType.Create(ClientId, "Manager", "manager");
        var stranger = Stakeholder.Create(Guid.CreateVersion7(), ClientId, Guid.CreateVersion7(), otherType.Id, "Sam", "Stranger");

        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        stakeholderRepository.GetByIdAsync(stranger.Id, Arg.Any<CancellationToken>()).Returns(stranger);
        var stakeholderTypeRepository = Substitute.For<IRepository<StakeholderType>>();
        stakeholderTypeRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<StakeholderType>>(), Arg.Any<CancellationToken>()).Returns(manangerType);

        var (handler, tenancy) = Build(stakeholderRepository, stakeholderTypeRepository);

        var result = await handler.HandleAsync(
            new ListTenancyDocumentsCommand(tenancy.Id, new ActorContext(stranger.Id, ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListTenancyDocumentsStatus.Forbidden);
    }
}

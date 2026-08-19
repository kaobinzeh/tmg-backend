using FluentValidation;
using TMG.Application.Authentication.Stakeholders;
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Application.Tenancies.Features.ActivateTenancy;
using TMG.Application.Tenancies.Features.AllocateUnit;
using TMG.Application.Tenancies.Features.GetTenancyCycle;
using TMG.Application.Tenancies.Features.GetTenancySummary;
using TMG.Application.Tenancies.Features.ListMyTenancies;
using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Application.Tenancies.Features.ListTenancyRentPayments;
using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Application.Tenancies.Features.RecordRentPayment;
using TMG.Application.Tenancies.Features.RejectTenancyInvitation;
using TMG.Application.Tenancies.Features.UploadTenancyDocument;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Storage;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;
using TMG.WebAPI.Features.Tenancies;

namespace TMG.WebAPI.UnitTests.Features.Tenancies;

internal sealed class TenanciesControllerTestContext
{
    public IAuthenticationIdentityService IdentityService { get; } = Substitute.For<IAuthenticationIdentityService>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IRepository<StakeholderType> StakeholderTypeRepository { get; } = Substitute.For<IRepository<StakeholderType>>();
    public IRepository<Property> PropertyRepository { get; } = Substitute.For<IRepository<Property>>();
    public IRepository<Unit> UnitRepository { get; } = Substitute.For<IRepository<Unit>>();
    public IRepository<Tenancy> TenancyRepository { get; } = Substitute.For<IRepository<Tenancy>>();
    public IRepository<TenancyDocument> TenancyDocumentRepository { get; } = Substitute.For<IRepository<TenancyDocument>>();
    public IRepository<RentPayment> RentPaymentRepository { get; } = Substitute.For<IRepository<RentPayment>>();
    public IStakeholderReadModelRepository StakeholderReadModelRepository { get; } = Substitute.For<IStakeholderReadModelRepository>();
    public ITenancyReadModelRepository TenancyReadModelRepository { get; } = Substitute.For<ITenancyReadModelRepository>();
    public ITenancyAgreementArchiver TenancyAgreementArchiver { get; } = Substitute.For<ITenancyAgreementArchiver>();
    public IRentReceiptArchiver RentReceiptArchiver { get; } = Substitute.For<IRentReceiptArchiver>();
    public IObjectStorageService ObjectStorageService { get; } = Substitute.For<IObjectStorageService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public ITwoFactorOtpService TwoFactorOtpService { get; } = Substitute.For<ITwoFactorOtpService>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 13, 0, 0, TimeSpan.Zero));

    public TenanciesController CreateController() =>
        new(
            new AllocateUnitHandler(
                IdentityService,
                StakeholderRepository,
                StakeholderTypeRepository,
                PropertyRepository,
                UnitRepository,
                TenancyRepository,
                EventPublisher,
                UnitOfWork),
            new AcceptTenancyInvitationHandler(
                IdentityService,
                new StakeholderResolver(StakeholderRepository),
                StakeholderRepository,
                TenancyRepository,
                UnitRepository,
                PropertyRepository,
                TenancyAgreementArchiver,
                EventPublisher,
                TwoFactorOtpService,
                UnitOfWork,
                Clock),
            new RejectTenancyInvitationHandler(
                IdentityService,
                new StakeholderResolver(StakeholderRepository),
                TenancyRepository,
                UnitRepository,
                TwoFactorOtpService,
                UnitOfWork,
                Clock),
            new UploadTenancyDocumentHandler(
                TenancyRepository,
                TenancyDocumentRepository,
                ObjectStorageService,
                UnitOfWork),
            new ActivateTenancyHandler(TenancyRepository, UnitOfWork),
            new RecordRentPaymentHandler(
                TenancyRepository,
                UnitRepository,
                PropertyRepository,
                RentPaymentRepository,
                StakeholderReadModelRepository,
                RentReceiptArchiver,
                EventPublisher,
                UnitOfWork,
                Clock),
            new ListTenancyRentPaymentsHandler(TenancyRepository, TenancyReadModelRepository),
            new GetTenancyCycleHandler(TenancyRepository),
            new GetTenancySummaryHandler(TenancyReadModelRepository, Clock),
            new ListTenancyAllocationsHandler(TenancyReadModelRepository),
            new ListMyTenanciesHandler(TenancyReadModelRepository),
            new ListUpcomingRenewalsHandler(TenancyReadModelRepository, Clock),
            Substitute.For<IValidator<AllocateUnitRequest>>(),
            Substitute.For<IValidator<AcceptTenancyInvitationRequest>>(),
            Substitute.For<IValidator<RejectTenancyInvitationRequest>>(),
            Substitute.For<IValidator<RecordRentPaymentRequest>>(),
            CurrentActor);

    public void AuthenticateActor(Guid stakeholderId, Guid clientId)
    {
        CurrentActor.ActorId.Returns(stakeholderId.ToString());
        CurrentActor.ClientId.Returns(clientId);
        CurrentActor.CorrelationId.Returns("correlation-id");
        CurrentActor.FlowId.Returns("flow-id");
    }

    public TenancyAllocationReadModel CreateAllocation(TenancyStatus status) =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            status,
            "Ada Lovelace",
            "ada@example.com",
            "Unit 1A",
            "Lekki Court",
            500000m,
            Guid.CreateVersion7(),
            Clock.GetUtcNow(),
            Clock.GetUtcNow().AddMonths(12),
            Clock.GetUtcNow().AddMonths(12));

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

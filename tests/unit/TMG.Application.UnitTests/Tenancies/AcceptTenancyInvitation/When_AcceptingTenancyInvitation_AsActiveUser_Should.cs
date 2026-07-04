using TMG.Application.Authentication.Stakeholders;
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.AcceptTenancyInvitation;

public sealed class When_AcceptingTenancyInvitation_AsActiveUser_Should
{
    [Fact]
    public async Task AcceptWithoutResettingPassword()
    {
        const string email = "returning@example.com";
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var unitRepository = Substitute.For<IRepository<Unit>>();
        var propertyRepository = Substitute.For<IRepository<Property>>();
        var agreementArchiver = Substitute.For<ITenancyAgreementArchiver>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var stakeholderResolver = new StakeholderResolver(stakeholderRepository);

        var user = AppUser.Create(email, "Ada", "Lovelace");
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada", "Lovelace");
        var tenancy = Tenancy.Invite(stakeholder.ClientId, Guid.CreateVersion7(), Guid.CreateVersion7(), stakeholder.Id, email);

        identityService.FindByEmailAsync(email).Returns(user);
        stakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        tenancyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Tenancy>>(), Arg.Any<CancellationToken>()).Returns(tenancy);
        twoFactorOtpService.ValidateOtpAsync(user.Id, "token-123", OtpIntent.TenancyInvitation, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new AcceptTenancyInvitationHandler(
            identityService, stakeholderResolver, stakeholderRepository, tenancyRepository,
            unitRepository, propertyRepository, agreementArchiver, eventPublisher,
            twoFactorOtpService, unitOfWork, TimeProvider.System);

        var result = await handler.HandleAsync(
            new AcceptTenancyInvitationCommand(email, "token-123", null, null, true, new ActorContext(null, stakeholder.ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(AcceptTenancyInvitationStatus.Success);
        tenancy.Status.ShouldBe(TenancyStatus.Accepted);
        await identityService.DidNotReceive().ResetPasswordAsync(Arg.Any<AppUser>(), Arg.Any<string>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

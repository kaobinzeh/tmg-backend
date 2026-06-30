using TMG.Application.Authentication.Stakeholders;
using TMG.Application.Tenancies.Features.AcceptTenancyInvitation;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.AcceptTenancyInvitation;

public sealed class When_AcceptingTenancyInvitation_WithValidToken_Should
{
    [Fact]
    public async Task SetPasswordConfirmEmailAndAcceptTenancy()
    {
        const string email = "tenant@example.com";
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderResolver = new StakeholderResolver(stakeholderRepository);

        var user = AppUser.Create(email, "Ada", "Lovelace");
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada", "Lovelace");
        var tenancy = Tenancy.Invite(stakeholder.ClientId, Guid.CreateVersion7(), Guid.CreateVersion7(), stakeholder.Id, email);

        identityService.FindByEmailAsync(email).Returns(user);
        stakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        tenancyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Tenancy>>(), Arg.Any<CancellationToken>()).Returns(tenancy);
        twoFactorOtpService.ValidateOtpAsync(user.Id, "token-123", OtpIntent.TenancyInvitation, Arg.Any<CancellationToken>()).Returns(true);
        identityService.ResetPasswordAsync(user, "P@ssw0rd123!").Returns(IdentityResult.Success);
        identityService.UpdateAsync(user).Returns(IdentityResult.Success);

        var handler = new AcceptTenancyInvitationHandler(
            identityService, stakeholderResolver, stakeholderRepository, tenancyRepository, twoFactorOtpService, unitOfWork, TimeProvider.System);

        var result = await handler.HandleAsync(
            new AcceptTenancyInvitationCommand(email, "token-123", "P@ssw0rd123!", "P@ssw0rd123!", true, new ActorContext(null, stakeholder.ClientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(AcceptTenancyInvitationStatus.Success);
        user.EmailConfirmed.ShouldBeTrue();
        stakeholder.IsVerified.ShouldBeTrue();
        tenancy.Status.ShouldBe(TenancyStatus.Accepted);
        await identityService.Received(1).ResetPasswordAsync(user, "P@ssw0rd123!");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

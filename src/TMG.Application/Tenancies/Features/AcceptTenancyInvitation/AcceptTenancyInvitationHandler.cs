using TMG.Application.Authentication.Features.CompletePasswordReset;
using TMG.Application.Authentication.Stakeholders;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.AcceptTenancyInvitation;

public sealed class AcceptTenancyInvitationHandler(
    IAuthenticationIdentityService identityService,
    StakeholderResolver stakeholderResolver,
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<Tenancy> tenancyRepository,
    ITwoFactorOtpService twoFactorOtpService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AcceptTenancyInvitationResult> HandleAsync(AcceptTenancyInvitationCommand command, CancellationToken cancellationToken)
    {
        if (!command.AcceptedTerms)
        {
            return new AcceptTenancyInvitationResult(AcceptTenancyInvitationStatus.TermsNotAccepted);
        }

        var user = await identityService.FindByEmailAsync(command.Email);
        if (user is null)
        {
            return new AcceptTenancyInvitationResult(AcceptTenancyInvitationStatus.InvalidInvitation);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new InvitedTenancyByTenantSpecification(stakeholder.Id),
            cancellationToken);
        if (tenancy is null)
        {
            return new AcceptTenancyInvitationResult(AcceptTenancyInvitationStatus.InvalidInvitation);
        }

        if (!await twoFactorOtpService.ValidateOtpAsync(user.Id, command.Token, OtpIntent.TenancyInvitation, cancellationToken))
        {
            return new AcceptTenancyInvitationResult(AcceptTenancyInvitationStatus.InvalidToken);
        }

        // A pending (manager-provisioned) account is activated here: set the password and confirm the
        // email. An already-active account (a returning tenant) just accepts — no password reset.
        if (!user.EmailConfirmed)
        {
            if (string.IsNullOrWhiteSpace(command.Password))
            {
                return new AcceptTenancyInvitationResult(
                    AcceptTenancyInvitationStatus.ValidationFailed,
                    new Dictionary<string, string[]>
                    {
                        ["Password"] = ["A password is required to activate your account."]
                    });
            }

            var passwordResult = await identityService.ResetPasswordAsync(user, command.Password);
            if (!passwordResult.Succeeded)
            {
                return new AcceptTenancyInvitationResult(
                    AcceptTenancyInvitationStatus.ValidationFailed,
                    passwordResult.ToValidationDictionary());
            }

            user.MarkEmailVerified();
            await identityService.UpdateAsync(user);
        }

        stakeholder.MarkVerified();
        stakeholderRepository.Update(stakeholder);

        tenancy.Accept(timeProvider.GetUtcNow());
        tenancyRepository.Update(tenancy);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcceptTenancyInvitationResult(AcceptTenancyInvitationStatus.Success);
    }
}

using TMG.Application.Authentication.Features.CompletePasswordReset;
using TMG.Application.Authentication.Stakeholders;
using TMG.Contracts.Events;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.AcceptTenancyInvitation;

public sealed class AcceptTenancyInvitationHandler(
    IAuthenticationIdentityService identityService,
    StakeholderResolver stakeholderResolver,
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    IRepository<Property> propertyRepository,
    ITenancyAgreementArchiver tenancyAgreementArchiver,
    IEventPublisher eventPublisher,
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

        var acceptedAtUtc = timeProvider.GetUtcNow();
        tenancy.Accept(acceptedAtUtc);
        tenancyRepository.Update(tenancy);

        // Generate + archive the tenancy agreement, then notify the tenant it's available.
        var unit = await unitRepository.GetByIdAsync(tenancy.UnitId, cancellationToken);
        var property = await propertyRepository.GetByIdAsync(tenancy.PropertyId, cancellationToken);
        var unitLabel = unit?.Label ?? string.Empty;
        var propertyName = property?.Name ?? string.Empty;

        var agreementDocumentId = await tenancyAgreementArchiver.ArchiveAsync(
            tenancy.ClientId,
            tenancy.Id,
            new TenancyAgreementModel(
                $"{stakeholder.FirstName} {stakeholder.LastName}".Trim(),
                propertyName,
                unitLabel,
                unit?.RentAmount ?? 0m,
                tenancy.ProposedLeaseStartUtc,
                tenancy.ProposedTermMonths,
                acceptedAtUtc),
            cancellationToken);

        await eventPublisher.PublishAsync(
            new TenancyAgreementReady
            {
                TenancyId = tenancy.Id,
                UnitId = tenancy.UnitId,
                PropertyId = tenancy.PropertyId,
                AgreementDocumentId = agreementDocumentId,
                UnitLabel = unitLabel,
                PropertyName = propertyName,
                StakeholderId = tenancy.TenantStakeholderId,
                ClientId = tenancy.ClientId,
                FlowId = command.ActorContext.FlowId,
                OccuredAt = acceptedAtUtc
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcceptTenancyInvitationResult(AcceptTenancyInvitationStatus.Success);
    }
}

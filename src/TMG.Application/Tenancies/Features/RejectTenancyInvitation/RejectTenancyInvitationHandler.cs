using TMG.Application.Authentication.Stakeholders;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.RejectTenancyInvitation;

public sealed class RejectTenancyInvitationHandler(
    IAuthenticationIdentityService identityService,
    StakeholderResolver stakeholderResolver,
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    ITwoFactorOtpService twoFactorOtpService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RejectTenancyInvitationResult> HandleAsync(RejectTenancyInvitationCommand command, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(command.Email);
        if (user is null)
        {
            return new RejectTenancyInvitationResult(RejectTenancyInvitationStatus.InvalidInvitation);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new InvitedTenancyByTenantSpecification(stakeholder.Id),
            cancellationToken);
        if (tenancy is null)
        {
            return new RejectTenancyInvitationResult(RejectTenancyInvitationStatus.InvalidInvitation);
        }

        if (!await twoFactorOtpService.ValidateOtpAsync(user.Id, command.Token, OtpIntent.TenancyInvitation, cancellationToken))
        {
            return new RejectTenancyInvitationResult(RejectTenancyInvitationStatus.InvalidToken);
        }

        tenancy.Reject(timeProvider.GetUtcNow());
        tenancyRepository.Update(tenancy);

        var unit = await unitRepository.GetByIdAsync(tenancy.UnitId, cancellationToken);
        if (unit is not null)
        {
            unit.MarkAvailable();
            unitRepository.Update(unit);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RejectTenancyInvitationResult(RejectTenancyInvitationStatus.Success);
    }
}

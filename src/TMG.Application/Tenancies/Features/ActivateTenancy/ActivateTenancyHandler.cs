using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.ActivateTenancy;

public sealed class ActivateTenancyHandler(
    IRepository<Tenancy> tenancyRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<ActivateTenancyResult> HandleAsync(ActivateTenancyCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new ActivateTenancyResult(ActivateTenancyStatus.NotAuthenticated);
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(command.TenancyId, clientId, asTracking: true),
            cancellationToken);
        if (tenancy is null)
        {
            return new ActivateTenancyResult(ActivateTenancyStatus.TenancyNotFound);
        }

        if (tenancy.Status != TenancyStatus.Accepted)
        {
            return new ActivateTenancyResult(ActivateTenancyStatus.NotAccepted);
        }

        // Request values win; otherwise fall back to the terms the manager proposed at allocation.
        var startUtc = command.LeaseStartDate ?? tenancy.ProposedLeaseStartUtc;
        var termMonths = command.TermMonths ?? tenancy.ProposedTermMonths;
        if (startUtc is not { } start || termMonths is not { } term || term <= 0)
        {
            return new ActivateTenancyResult(ActivateTenancyStatus.MissingLeaseTerms);
        }

        tenancy.Activate(start, term);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivateTenancyResult(ActivateTenancyStatus.Success);
    }
}

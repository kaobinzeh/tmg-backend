using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.GetTenancyCycle;

public sealed class GetTenancyCycleHandler(IRepository<Tenancy> tenancyRepository)
{
    public async Task<GetTenancyCycleResult> HandleAsync(GetTenancyCycleCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new GetTenancyCycleResult(GetTenancyCycleStatus.NotAuthenticated);
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(command.TenancyId, clientId),
            cancellationToken);
        if (tenancy is null)
        {
            return new GetTenancyCycleResult(GetTenancyCycleStatus.TenancyNotFound);
        }

        return new GetTenancyCycleResult(
            GetTenancyCycleStatus.Success,
            new TenancyCycleDto(
                tenancy.Id,
                tenancy.UnitId,
                tenancy.Status,
                tenancy.CycleStartUtc,
                tenancy.CycleEndUtc,
                tenancy.NextRentDueUtc,
                tenancy.Reminder3MonthsSentAtUtc,
                tenancy.Reminder1MonthSentAtUtc));
    }
}

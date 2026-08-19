using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.ListTenancyRentPayments;

/// <summary>
/// Lists the rent payments recorded against a tenancy for the manager's tenancy-detail view.
/// Scoped to the caller's client; a tenancy that does not belong to the client reads as not found.
/// </summary>
public sealed class ListTenancyRentPaymentsHandler(
    IRepository<Tenancy> tenancyRepository,
    ITenancyReadModelRepository tenancyReadModelRepository)
{
    public async Task<ListTenancyRentPaymentsResult> HandleAsync(
        ListTenancyRentPaymentsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new ListTenancyRentPaymentsResult(ListTenancyRentPaymentsStatus.NotAuthenticated, []);
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(command.TenancyId, clientId),
            cancellationToken);
        if (tenancy is null)
        {
            return new ListTenancyRentPaymentsResult(ListTenancyRentPaymentsStatus.TenancyNotFound, []);
        }

        var payments = await tenancyReadModelRepository.ListRentPaymentsByTenancyAsync(
            clientId,
            command.TenancyId,
            cancellationToken);

        return new ListTenancyRentPaymentsResult(
            ListTenancyRentPaymentsStatus.Success,
            payments.Select(RentPaymentListItem.FromReadModel).ToList());
    }
}

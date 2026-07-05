using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;
using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Properties.Features.ListUnits;

public sealed class ListUnitsHandler(
    IRepository<Property> propertyRepository,
    IRepository<Unit> unitRepository,
    ITenancyReadModelRepository tenancyReadModelRepository)
{
    public async Task<ListUnitsResult> HandleAsync(ListUnitsCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new ListUnitsResult(ListUnitsStatus.NotAuthenticated, []);
        }

        var propertyExists = await propertyRepository.AnyAsync(
            new PropertyByIdSpecification(command.PropertyId, clientId),
            cancellationToken);

        if (!propertyExists)
        {
            return new ListUnitsResult(ListUnitsStatus.PropertyNotFound, []);
        }

        var units = await unitRepository.ListAsync(
            new UnitsByPropertySpecification(command.PropertyId, clientId),
            cancellationToken);

        var currentTenancies = await tenancyReadModelRepository.ListCurrentByPropertyAsync(
            clientId,
            command.PropertyId,
            cancellationToken);
        var tenanciesByUnit = currentTenancies
            .GroupBy(tenancy => tenancy.UnitId)
            .ToDictionary(group => group.Key, group => group.First());

        var items = units
            .Select(unit =>
            {
                var currentTenancy = tenanciesByUnit.GetValueOrDefault(unit.Id);
                return new UnitListItem(
                    unit.Id,
                    unit.Label,
                    unit.Description,
                    unit.NumberOfRooms,
                    unit.RentAmount,
                    unit.CurrencyId,
                    unit.Status,
                    currentTenancy?.TenancyId,
                    currentTenancy?.Status.ToString(),
                    currentTenancy?.TenantName,
                    currentTenancy?.TenantEmail);
            })
            .ToList();

        return new ListUnitsResult(ListUnitsStatus.Success, items);
    }
}

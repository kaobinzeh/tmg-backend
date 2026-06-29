using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;

namespace TMG.Application.Properties.Features.SetUnitAvailability;

public sealed class SetUnitAvailabilityHandler(
    IRepository<Unit> unitRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<SetUnitAvailabilityResult> HandleAsync(SetUnitAvailabilityCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new SetUnitAvailabilityResult(SetUnitAvailabilityStatus.NotAuthenticated);
        }

        var unit = await unitRepository.FirstOrDefaultAsync(
            new UnitByIdSpecification(command.UnitId, clientId),
            cancellationToken);

        if (unit is null)
        {
            return new SetUnitAvailabilityResult(SetUnitAvailabilityStatus.UnitNotFound);
        }

        switch (command.Status)
        {
            case UnitStatus.Available:
                unit.MarkAvailable();
                break;
            case UnitStatus.Occupied:
                unit.MarkOccupied();
                break;
            default:
                unit.MarkUnavailable();
                break;
        }

        unitRepository.Update(unit);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetUnitAvailabilityResult(SetUnitAvailabilityStatus.Success);
    }
}

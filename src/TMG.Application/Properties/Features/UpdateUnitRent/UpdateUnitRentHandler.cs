using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;

namespace TMG.Application.Properties.Features.UpdateUnitRent;

public sealed class UpdateUnitRentHandler(
    IRepository<Unit> unitRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdateUnitRentResult> HandleAsync(UpdateUnitRentCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new UpdateUnitRentResult(UpdateUnitRentStatus.NotAuthenticated);
        }

        var unit = await unitRepository.FirstOrDefaultAsync(
            new UnitByIdSpecification(command.UnitId, clientId),
            cancellationToken);

        if (unit is null)
        {
            return new UpdateUnitRentResult(UpdateUnitRentStatus.UnitNotFound);
        }

        unit.UpdateRent(command.RentAmount);
        unitRepository.Update(unit);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateUnitRentResult(UpdateUnitRentStatus.Success);
    }
}

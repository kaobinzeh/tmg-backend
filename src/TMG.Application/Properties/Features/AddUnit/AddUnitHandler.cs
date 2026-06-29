using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;

namespace TMG.Application.Properties.Features.AddUnit;

public sealed class AddUnitHandler(
    IRepository<Property> propertyRepository,
    IRepository<Unit> unitRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<AddUnitResult> HandleAsync(AddUnitCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new AddUnitResult(AddUnitStatus.NotAuthenticated);
        }

        var propertyExists = await propertyRepository.AnyAsync(
            new PropertyByIdSpecification(command.PropertyId, clientId),
            cancellationToken);

        if (!propertyExists)
        {
            return new AddUnitResult(AddUnitStatus.PropertyNotFound);
        }

        var labelTaken = await unitRepository.AnyAsync(
            new UnitByPropertyAndLabelSpecification(command.PropertyId, command.Label),
            cancellationToken);

        if (labelTaken)
        {
            return new AddUnitResult(AddUnitStatus.DuplicateLabel);
        }

        var unit = Unit.Create(
            command.PropertyId,
            clientId,
            command.Label,
            command.Description,
            command.NumberOfRooms,
            command.RentAmount,
            command.CurrencyId);

        await unitRepository.AddAsync(unit, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddUnitResult(AddUnitStatus.Success, unit.Id);
    }
}

using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;

namespace TMG.Application.Properties.Features.CreateProperty;

public sealed class CreatePropertyHandler(
    IRepository<Property> propertyRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<CreatePropertyResult> HandleAsync(CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } ownerStakeholderId)
        {
            return new CreatePropertyResult(CreatePropertyStatus.NotAuthenticated);
        }

        var property = Property.Create(
            clientId,
            ownerStakeholderId,
            command.Name,
            command.Address,
            command.Description);

        await propertyRepository.AddAsync(property, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePropertyResult(CreatePropertyStatus.Success, property.Id);
    }
}

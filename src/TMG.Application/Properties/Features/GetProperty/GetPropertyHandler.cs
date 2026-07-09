using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;

namespace TMG.Application.Properties.Features.GetProperty;

public sealed class GetPropertyHandler(IRepository<Property> propertyRepository)
{
    public async Task<GetPropertyResult> HandleAsync(GetPropertyCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new GetPropertyResult(GetPropertyStatus.NotAuthenticated, null);
        }

        var property = await propertyRepository.FirstOrDefaultAsync(
            new PropertyByIdSpecification(command.PropertyId, clientId),
            cancellationToken);
        if (property is null)
        {
            return new GetPropertyResult(GetPropertyStatus.PropertyNotFound, null);
        }

        return new GetPropertyResult(
            GetPropertyStatus.Success,
            new PropertyDetail(
                property.Id,
                property.Name,
                property.Address,
                property.Description));
    }
}

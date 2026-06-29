using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;

namespace TMG.Application.Properties.Features.ListProperties;

public sealed class ListPropertiesHandler(IRepository<Property> propertyRepository)
{
    public async Task<ListPropertiesResult> HandleAsync(ListPropertiesCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new ListPropertiesResult(ListPropertiesStatus.NotAuthenticated, []);
        }

        var properties = await propertyRepository.ListAsync(
            new PropertiesByClientSpecification(clientId),
            cancellationToken);

        var items = properties
            .Select(property => new PropertyListItem(
                property.Id,
                property.Name,
                property.Address,
                property.Description))
            .ToList();

        return new ListPropertiesResult(ListPropertiesStatus.Success, items);
    }
}

using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Specifications;
using TMG.Domain.Common.Persistence;

namespace TMG.Consumer.Authentication;

public sealed class LoginActivityIpAddressResolver(
    IRepository<IpAddress> ipAddressRepository) : ILoginActivityIpAddressResolver
{
    public async Task<LoginActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var existingIpAddress = await ipAddressRepository.FirstOrDefaultAsync(
            new IpAddressByValueSpecification(ipAddress),
            cancellationToken);

        var persistedIpAddress = existingIpAddress;
        if (persistedIpAddress is null)
        {
            persistedIpAddress = IpAddress.Create(ipAddress);
            await ipAddressRepository.AddAsync(persistedIpAddress, cancellationToken);
        }

        return new LoginActivityIpAddressResolution(
            persistedIpAddress.Id,
            persistedIpAddress.GetCurrentLocation()?.Id);
    }
}

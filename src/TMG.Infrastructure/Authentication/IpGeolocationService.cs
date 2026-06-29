using TMG.Domain.Authentication.Services;

namespace TMG.Infrastructure.Authentication;

internal sealed class IpGeolocationService(IEnumerable<IIpGeolocationProvider> providers) : IIpGeolocationService
{
    public async Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken)
    {
        if (!IpAddressUtility.IsPublicIpAddress(ipAddress))
        {
            return null;
        }

        foreach (var provider in providers)
        {
            var geolocation = await provider.GetGeolocationAsync(ipAddress, cancellationToken);
            if (geolocation is not null)
            {
                return geolocation;
            }
        }

        return null;
    }
}

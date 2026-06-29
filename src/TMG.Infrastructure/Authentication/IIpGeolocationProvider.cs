using TMG.Domain.Authentication.Services;

namespace TMG.Infrastructure.Authentication;

internal interface IIpGeolocationProvider
{
    Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken);
}

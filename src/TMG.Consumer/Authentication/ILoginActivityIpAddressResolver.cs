namespace TMG.Consumer.Authentication;

public interface ILoginActivityIpAddressResolver
{
    Task<LoginActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken);
}

namespace TMG.Domain.Common.Authentication;

public interface IGoogleIdentityTokenService
{
    Task<GoogleIdentityTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}

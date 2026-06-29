using TMG.Domain.Authentication.Entities;

namespace TMG.Domain.Common.Authentication;

public interface IRefreshTokenService
{
    Task<RefreshToken> IssueAsync(AppUser user, CancellationToken cancellationToken);
    Task<AuthenticationRefreshToken?> FindByTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken> RotateAsync(AuthenticationRefreshToken currentRefreshToken, AppUser user, CancellationToken cancellationToken);
    void Revoke(AuthenticationRefreshToken refreshToken, DateTimeOffset utcNow);
}

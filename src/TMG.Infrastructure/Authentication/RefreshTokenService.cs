using System.Security.Cryptography;
using System.Text;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Specifications;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace TMG.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    IRepository<AuthenticationRefreshToken> refreshTokenRepository,
    IAuthenticationIdentityService identityService,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider) : IRefreshTokenService
{
    private readonly RefreshTokenOptions _options = options.Value;

    public async Task<RefreshToken> IssueAsync(AppUser user, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        var rawToken = GenerateRawToken();
        var expiresAtUtc = utcNow.AddDays(_options.LifetimeDays);
        var securityStamp = await GetRequiredSecurityStampAsync(user, cancellationToken);
        var refreshToken = AuthenticationRefreshToken.Create(user.Id, ComputeHash(rawToken), securityStamp, expiresAtUtc);

        await refreshTokenRepository.AddAsync(refreshToken);

        return new RefreshToken(rawToken, expiresAtUtc);
    }

    public Task<AuthenticationRefreshToken?> FindByTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
        refreshTokenRepository.FirstOrDefaultAsync(
            new RefreshTokenByHashSpecification(ComputeHash(refreshToken)),
            cancellationToken);

    public async Task<RefreshToken> RotateAsync(AuthenticationRefreshToken currentRefreshToken, AppUser user, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        Revoke(currentRefreshToken, utcNow);
        refreshTokenRepository.Update(currentRefreshToken);

        return await IssueAsync(user, cancellationToken);
    }

    public void Revoke(AuthenticationRefreshToken refreshToken, DateTimeOffset utcNow)
    {
        refreshToken.Revoke(utcNow);
        refreshTokenRepository.Update(refreshToken);
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHash(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<string> GetRequiredSecurityStampAsync(AppUser user, CancellationToken cancellationToken)
    {
        var securityStamp = await identityService.GetSecurityStampAsync(user);
        if (string.IsNullOrWhiteSpace(securityStamp))
        {
            throw new InvalidOperationException($"Security stamp is missing for user '{user.Id}'.");
        }

        return securityStamp;
    }
}


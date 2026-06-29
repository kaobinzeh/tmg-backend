using System.IdentityModel.Tokens.Jwt;
using TMG.Domain.Common.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace TMG.Infrastructure.Authentication;

public sealed class GoogleIdentityTokenService(
    IOptions<GoogleAuthenticationOptions> options)
    : IGoogleIdentityTokenService
{
    private static readonly string[] ValidIssuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];

    private static readonly ConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager = new(
        "https://accounts.google.com/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever
        {
            RequireHttps = true
        });

    public async Task<GoogleIdentityTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var configuredClientIds = options.Value.ClientIds
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
            .Select(clientId => clientId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (configuredClientIds.Length == 0)
        {
            throw new InvalidOperationException("Google authentication client ids are not configured.");
        }

        var openIdConfiguration = await ConfigurationManager.GetConfigurationAsync(cancellationToken);
        var tokenHandler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        try
        {
            var principal = tokenHandler.ValidateToken(
                idToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = ValidIssuers,
                    ValidateAudience = true,
                    ValidAudiences = configuredClientIds,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = openIdConfiguration.SigningKeys,
                    ClockSkew = TimeSpan.FromMinutes(1)
                },
                out _);

            var email = principal.FindFirst("email")?.Value?.Trim();
            var subject = principal.FindFirst("sub")?.Value?.Trim();
            var emailVerified = bool.TryParse(principal.FindFirst("email_verified")?.Value, out var parsedEmailVerified)
                && parsedEmailVerified;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subject) || !emailVerified)
            {
                return null;
            }

            return new GoogleIdentityTokenPayload(
                subject,
                email,
                principal.FindFirst("name")?.Value?.Trim());
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

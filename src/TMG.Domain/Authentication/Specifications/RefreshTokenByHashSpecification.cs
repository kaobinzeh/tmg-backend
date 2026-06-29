using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Persistence;

namespace TMG.Domain.Authentication.Specifications;

public sealed class RefreshTokenByHashSpecification : Specification<AuthenticationRefreshToken>
{
    public RefreshTokenByHashSpecification(string tokenHash)
    {
        Where(refreshToken => refreshToken.TokenHash == tokenHash);
        ApplyPaging(0, 1);
    }
}

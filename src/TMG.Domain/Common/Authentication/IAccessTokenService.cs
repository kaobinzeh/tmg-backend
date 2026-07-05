using TMG.Domain.Authentication.Entities;

namespace TMG.Domain.Common.Authentication;

public interface IAccessTokenService
{
    AccessToken Generate(AppUser user, Guid stakeholderId, string stakeholderTypeKey);
}

using TMG.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace TMG.Domain.Common.Authentication;

public interface IAuthenticationIdentityService
{
    Task<AppUser?> FindByIdAsync(Guid userId);
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByLoginAsync(string loginProvider, string providerKey);
    Task<string> GetSecurityStampAsync(AppUser user);
    Task<bool> IsLockedOutAsync(AppUser user);
    Task<DateTimeOffset?> GetLockoutEndUtcAsync(AppUser user);
    Task<IdentityResult> CreateAsync(AppUser user);
    Task<IdentityResult> CreateAsync(AppUser user, string password);
    Task<IdentityResult> AddLoginAsync(AppUser user, string loginProvider, string providerKey, string displayName);
    Task<string> GenerateSignUpOtpAsync(AppUser user);
    Task<bool> VerifySignUpOtpAsync(AppUser user, string otp);
    Task<IdentityResult> ResetPasswordAsync(AppUser user, string newPassword);
    Task<bool> CheckPasswordAsync(AppUser user, string password);
    Task<IdentityResult> AccessFailedAsync(AppUser user);
    Task<IdentityResult> ResetAccessFailedCountAsync(AppUser user);
    Task<IdentityResult> UpdateAsync(AppUser user);
}

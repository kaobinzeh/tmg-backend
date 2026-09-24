using TMG.Domain.Common.Authentication;
using TMG.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace TMG.Infrastructure.Authentication;

public sealed class IdentityUserService(UserManager<AppUser> userManager) : IAuthenticationIdentityService
{
    public Task<AppUser?> FindByIdAsync(Guid userId) =>
        userManager.FindByIdAsync(userId.ToString());

    public Task<AppUser?> FindByEmailAsync(string email) =>
        userManager.FindByEmailAsync(email);

    public Task<AppUser?> FindByLoginAsync(string loginProvider, string providerKey) =>
        userManager.FindByLoginAsync(loginProvider, providerKey);

    public Task<string> GetSecurityStampAsync(AppUser user) =>
        userManager.GetSecurityStampAsync(user);

    public Task<bool> IsLockedOutAsync(AppUser user) =>
        userManager.IsLockedOutAsync(user);

    public async Task<DateTimeOffset?> GetLockoutEndUtcAsync(AppUser user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        return lockoutEnd?.ToUniversalTime();
    }

    public Task<IdentityResult> CreateAsync(AppUser user) =>
        userManager.CreateAsync(user);

    public Task<IdentityResult> CreateAsync(AppUser user, string password) =>
        userManager.CreateAsync(user, password);

    public Task<IdentityResult> AddLoginAsync(AppUser user, string loginProvider, string providerKey, string displayName) =>
        userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, displayName));

    public async Task<IdentityResult> ResetPasswordAsync(AppUser user, string newPassword)
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        return await userManager.ResetPasswordAsync(user, resetToken, newPassword);
    }

    public Task<bool> CheckPasswordAsync(AppUser user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    public Task<IdentityResult> AccessFailedAsync(AppUser user) =>
        userManager.AccessFailedAsync(user);

    public Task<IdentityResult> ResetAccessFailedCountAsync(AppUser user) =>
        userManager.ResetAccessFailedCountAsync(user);

    public Task<IdentityResult> UpdateAsync(AppUser user) =>
        userManager.UpdateAsync(user);
}

using TMG.Domain.Authentication.Entities;

namespace TMG.Domain.Authentication.Persistence;

public interface IAppUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    void Remove(AppUser user);
}

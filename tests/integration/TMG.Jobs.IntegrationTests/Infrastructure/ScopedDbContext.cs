using TMG.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace TMG.Jobs.IntegrationTests.Infrastructure;

public sealed class ScopedDbContext : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;

    public ScopedDbContext(AsyncServiceScope scope)
    {
        _scope = scope;
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public AppDbContext DbContext { get; }

    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}

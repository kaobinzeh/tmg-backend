using Microsoft.Extensions.DependencyInjection;
using AppDbContext = TMG.Infrastructure.Persistence.AppDbContext;

namespace TMG.Consumer.IntegrationTests.Infrastructure;

public sealed class ScopedDbContext(IServiceScope scope) : IDisposable
{
    public AppDbContext DbContext { get; } = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    public void Dispose() => scope.Dispose();
}

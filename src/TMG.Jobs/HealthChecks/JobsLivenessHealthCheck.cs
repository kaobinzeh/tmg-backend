using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TMG.Jobs.HealthChecks;

public sealed class JobsLivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Jobs host is alive."));
}

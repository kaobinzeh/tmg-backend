using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Persistence;

namespace TMG.Domain.Authentication.Specifications;

public sealed class PendingIpAddressLocationEnrichmentSpecification : Specification<IpAddress>
{
    public PendingIpAddressLocationEnrichmentSpecification(int batchSize, DateTimeOffset staleThresholdUtc)
    {
        Where(ipAddress =>
            ipAddress.LocationLookupTimestampUtc == null ||
            ipAddress.LocationLookupTimestampUtc < staleThresholdUtc);
        AddInclude(ipAddress => ipAddress.Locations);
        ApplyOrderBy(ipAddress => ipAddress.CreatedAtUtc);
        ApplyPaging(0, batchSize);
        EnableTracking();
    }
}

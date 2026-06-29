using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Persistence;

namespace TMG.Domain.Authentication.Specifications;

public sealed class IpAddressByValueSpecification : Specification<IpAddress>
{
    public IpAddressByValueSpecification(string value)
    {
        Where(ipAddress => ipAddress.Value == value);
        AddInclude(ipAddress => ipAddress.Locations);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}

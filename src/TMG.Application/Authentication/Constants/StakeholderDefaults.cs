namespace TMG.Application.Authentication.Constants;

/// <summary>
/// Defines the stakeholder role types available in the tenancy management domain.
/// Each type maps to a StakeholderType record seeded per client.
/// </summary>
public static class StakeholderDefaults
{
    public static class Types
    {
        public const string LandlordName = "Landlord";
        public const string LandlordKey = "landlord";

        public const string ManagerName = "Manager";
        public const string ManagerKey = "manager";

        public const string LawyerName = "Lawyer";
        public const string LawyerKey = "lawyer";

        public const string TenantName = "Tenant";
        public const string TenantKey = "tenant";

        /// <summary>All valid stakeholder type keys. Used for validation at the API boundary.</summary>
        public static readonly IReadOnlyList<string> ValidKeys =
        [
            LandlordKey,
            ManagerKey,
            LawyerKey,
            TenantKey,
        ];
    }
}

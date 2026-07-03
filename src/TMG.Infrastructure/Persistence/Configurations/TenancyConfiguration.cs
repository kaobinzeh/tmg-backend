using TMG.Domain.Tenancies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class TenancyConfiguration : IEntityTypeConfiguration<Tenancy>
{
    public void Configure(EntityTypeBuilder<Tenancy> builder)
    {
        builder.ToTable("Tenancies", SchemaNames.Tenancies);
        builder.HasKey(tenancy => tenancy.Id);

        builder.Property(tenancy => tenancy.ClientId).IsRequired();
        builder.Property(tenancy => tenancy.PropertyId).IsRequired();
        builder.Property(tenancy => tenancy.UnitId).IsRequired();
        builder.Property(tenancy => tenancy.TenantStakeholderId).IsRequired();
        builder.Property(tenancy => tenancy.InvitedEmail).HasMaxLength(256).IsRequired();
        builder.Property(tenancy => tenancy.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(tenancy => tenancy.ClientId);
        builder.HasIndex(tenancy => tenancy.UnitId);
        builder.HasIndex(tenancy => tenancy.TenantStakeholderId);

        // Supports the rent-reminder scan, which filters by Status and orders by NextRentDueUtc.
        builder.HasIndex(tenancy => new { tenancy.Status, tenancy.NextRentDueUtc });
    }
}

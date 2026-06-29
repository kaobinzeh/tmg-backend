using TMG.Domain.Properties.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties", SchemaNames.Properties);
        builder.HasKey(property => property.Id);

        builder.Property(property => property.ClientId).IsRequired();
        builder.Property(property => property.OwnerStakeholderId).IsRequired();
        builder.Property(property => property.Name).HasMaxLength(200).IsRequired();
        builder.Property(property => property.Address).HasMaxLength(500).IsRequired();
        builder.Property(property => property.Description).HasMaxLength(2000);

        builder.HasIndex(property => property.ClientId);
        builder.HasIndex(property => property.OwnerStakeholderId);
    }
}

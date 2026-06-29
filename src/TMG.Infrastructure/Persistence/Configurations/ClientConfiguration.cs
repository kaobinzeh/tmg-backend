using TMG.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients", SchemaNames.Stakeholders);
        builder.HasKey(client => client.Id);

        builder.Property(client => client.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(client => client.BrandKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(client => client.BrandKey)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

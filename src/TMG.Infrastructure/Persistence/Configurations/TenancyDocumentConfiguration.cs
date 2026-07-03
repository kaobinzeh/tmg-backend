using TMG.Domain.Tenancies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class TenancyDocumentConfiguration : IEntityTypeConfiguration<TenancyDocument>
{
    public void Configure(EntityTypeBuilder<TenancyDocument> builder)
    {
        builder.ToTable("TenancyDocuments", SchemaNames.Tenancies);
        builder.HasKey(document => document.Id);

        builder.Property(document => document.ClientId).IsRequired();
        builder.Property(document => document.TenancyId).IsRequired();
        builder.Property(document => document.DocumentType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(document => document.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.UploadedByStakeholderId);

        builder.HasIndex(document => document.TenancyId);
        builder.HasIndex(document => document.ClientId);

        builder.HasOne<Tenancy>()
            .WithMany()
            .HasForeignKey(document => document.TenancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

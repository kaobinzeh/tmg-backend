using TMG.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class ClientEmailBaseTemplateConfiguration : IEntityTypeConfiguration<ClientEmailBaseTemplate>
{
    public void Configure(EntityTypeBuilder<ClientEmailBaseTemplate> builder)
    {
        builder.ToTable("ClientEmailBaseTemplates", SchemaNames.Notifications);

        builder.HasKey(template => template.Id);

        builder.Property(template => template.ClientId)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(template => template.HtmlTemplate)
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(template => template.ClientId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

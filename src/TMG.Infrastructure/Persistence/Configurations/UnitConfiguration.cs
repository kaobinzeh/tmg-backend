using TMG.Domain.Properties.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units", SchemaNames.Properties);
        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.PropertyId).IsRequired();
        builder.Property(unit => unit.ClientId).IsRequired();
        builder.Property(unit => unit.Label).HasMaxLength(100).IsRequired();
        builder.Property(unit => unit.Description).HasMaxLength(2000);
        builder.Property(unit => unit.NumberOfRooms).IsRequired();
        builder.Property(unit => unit.RentAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(unit => unit.CurrencyId).IsRequired();
        builder.Property(unit => unit.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(unit => unit.PropertyId);
        builder.HasIndex(unit => unit.ClientId);
        builder.HasIndex(unit => unit.CurrencyId);
        builder.HasIndex(unit => new { unit.PropertyId, unit.Label })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(unit => unit.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using TMG.Domain.Tenancies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TMG.Infrastructure.Persistence.Configurations;

public sealed class RentPaymentConfiguration : IEntityTypeConfiguration<RentPayment>
{
    public void Configure(EntityTypeBuilder<RentPayment> builder)
    {
        builder.ToTable("RentPayments", SchemaNames.Tenancies);
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.ClientId).IsRequired();
        builder.Property(payment => payment.TenancyId).IsRequired();
        builder.Property(payment => payment.UnitId).IsRequired();
        builder.Property(payment => payment.PropertyId).IsRequired();
        builder.Property(payment => payment.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.CurrencyId).IsRequired();
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.Reference).HasMaxLength(200);

        builder.HasIndex(payment => payment.TenancyId);
        builder.HasIndex(payment => payment.ClientId);

        builder.HasOne<Tenancy>()
            .WithMany()
            .HasForeignKey(payment => payment.TenancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

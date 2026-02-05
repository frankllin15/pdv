using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;

namespace PDV.Data.Local.Configurations;

public class FiscalConfigurationConfiguration : IEntityTypeConfiguration<FiscalConfiguration>
{
    public void Configure(EntityTypeBuilder<FiscalConfiguration> builder)
    {
        builder.ToTable("FiscalConfigurations");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.TaxId)
            .IsRequired()
            .HasMaxLength(14);

        builder.HasIndex(f => f.TaxId)
            .IsUnique();

        builder.Property(f => f.LegalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.TradeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.StateRegistration)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(f => f.State)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(f => f.CityCode)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(f => f.Address)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.AddressNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(f => f.Neighborhood)
            .HasMaxLength(100);

        builder.Property(f => f.ZipCode)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(f => f.TaxRegime)
            .IsRequired();

        builder.Property(f => f.Series)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(f => f.NextNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(f => f.CertificatePath)
            .HasMaxLength(500);

        builder.Property(f => f.CertificatePassword)
            .HasMaxLength(200);

        builder.Property(f => f.CscToken)
            .HasMaxLength(100);

        builder.Property(f => f.CscId)
            .HasMaxLength(10);

        builder.Property(f => f.IsProduction)
            .HasDefaultValue(false);

        builder.Property(f => f.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(f => f.IsActive);
    }
}

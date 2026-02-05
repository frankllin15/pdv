using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Configurations;

public class FiscalTransactionConfiguration : IEntityTypeConfiguration<FiscalTransaction>
{
    public void Configure(EntityTypeBuilder<FiscalTransaction> builder)
    {
        builder.ToTable("FiscalTransactions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.SaleId)
            .IsRequired();

        builder.Property(f => f.AccessKey)
            .IsRequired()
            .HasMaxLength(44);

        builder.HasIndex(f => f.AccessKey)
            .IsUnique();

        builder.Property(f => f.Number)
            .IsRequired();

        builder.Property(f => f.Series)
            .IsRequired();

        builder.Property(f => f.Status)
            .IsRequired()
            .HasDefaultValue(FiscalStatus.Pending);

        builder.Property(f => f.Protocol)
            .HasMaxLength(50);

        builder.Property(f => f.StatusCode);

        builder.Property(f => f.StatusMessage)
            .HasMaxLength(500);

        builder.Property(f => f.XmlRequest);

        builder.Property(f => f.XmlResponse);

        builder.Property(f => f.IsContingency)
            .HasDefaultValue(false);

        builder.Property(f => f.AuthorizationDate);

        builder.Property(f => f.CancellationDate);

        builder.Property(f => f.CancellationProtocol)
            .HasMaxLength(50);

        builder.Property(f => f.CancellationJustification)
            .HasMaxLength(255);

        builder.HasOne(f => f.Sale)
            .WithMany()
            .HasForeignKey(f => f.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.SaleId);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.IsContingency);
    }
}

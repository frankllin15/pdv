using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;

namespace PDV.Data.Local.Configurations;

public class FiscalReprintLogConfiguration : IEntityTypeConfiguration<FiscalReprintLog>
{
    public void Configure(EntityTypeBuilder<FiscalReprintLog> builder)
    {
        builder.ToTable("FiscalReprintLogs");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        builder.Property(f => f.FiscalTransactionId)
            .IsRequired();

        builder.Property(f => f.OperatorId)
            .IsRequired();

        builder.Property(f => f.ReprintedAt)
            .IsRequired();

        builder.Property(f => f.Reason)
            .HasMaxLength(255);

        builder.Property(f => f.ReprintNumber)
            .IsRequired();

        builder.HasOne(f => f.FiscalTransaction)
            .WithMany()
            .HasForeignKey(f => f.FiscalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Operator)
            .WithMany()
            .HasForeignKey(f => f.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(f => f.FiscalTransactionId);
        builder.HasIndex(f => f.OperatorId);
        builder.HasIndex(f => f.ReprintedAt);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Configurations;

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.ToTable("CashTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.CashSessionId)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("REAL");

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.OperatorId)
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .IsRequired();

        builder.Property(t => t.SyncState)
            .HasDefaultValue(SyncState.Pending);

        builder.HasIndex(t => t.CashSessionId);
    }
}

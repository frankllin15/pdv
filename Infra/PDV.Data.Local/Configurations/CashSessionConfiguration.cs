using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Configurations;

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("CashSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.OperatorId)
            .IsRequired();

        builder.Property(s => s.TerminalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.OpenedAt)
            .IsRequired();

        builder.Property(s => s.OpeningBalance)
            .HasColumnType("REAL");

        builder.Property(s => s.ClosingBalance)
            .HasColumnType("REAL");

        builder.Property(s => s.CalculatedBalance)
            .HasColumnType("REAL");

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.SyncState)
            .HasDefaultValue(SyncState.Pending);

        builder.Ignore(s => s.Difference);

        builder.HasMany(s => s.Transactions)
            .WithOne(t => t.CashSession)
            .HasForeignKey(t => t.CashSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(CashSession.Transactions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => new { s.TerminalId, s.Status });
    }
}

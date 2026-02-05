using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.SaleNumber)
            .IsRequired();

        builder.Property(s => s.SaleDate)
            .IsRequired();

        builder.Property(s => s.Subtotal)
            .HasColumnType("REAL");

        builder.Property(s => s.Discount)
            .HasColumnType("REAL");

        builder.Property(s => s.Total)
            .HasColumnType("REAL");

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.CustomerDocument)
            .HasMaxLength(20);

        builder.Property(s => s.SyncState)
            .HasDefaultValue(SyncState.Pending);

        // Fiscal fields
        builder.Property(s => s.FiscalStatus)
            .HasDefaultValue(FiscalStatus.None);

        builder.Property(s => s.FiscalAccessKey)
            .HasMaxLength(44);

        builder.Property(s => s.FiscalNumber);

        builder.Property(s => s.FiscalSeries);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Sale)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Payments)
            .WithOne(p => p.Sale)
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore the backing fields
        builder.Metadata.FindNavigation(nameof(Sale.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Sale.Payments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

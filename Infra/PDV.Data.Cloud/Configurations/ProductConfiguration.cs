using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Data.Cloud.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Barcode)
            .IsUnique();

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(50);

        builder.Property(p => p.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.UnitOfMeasure)
            .HasMaxLength(10)
            .HasDefaultValue("UN");

        builder.Property(p => p.StockQuantity)
            .HasColumnType("decimal(18,3)");

        builder.Property(p => p.TaxCode)
            .HasMaxLength(20);

        builder.Property(p => p.TaxRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.SyncState)
            .HasDefaultValue(SyncState.Pending);
    }
}

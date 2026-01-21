using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;

namespace PDV.Data.Local.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.SaleId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.Barcode)
            .HasMaxLength(50);

        builder.Property(i => i.ProductDescription)
            .HasMaxLength(200);

        builder.Property(i => i.Quantity)
            .HasColumnType("REAL")
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasColumnType("REAL")
            .IsRequired();

        builder.Property(i => i.Discount)
            .HasColumnType("REAL");

        builder.Property(i => i.Total)
            .HasColumnType("REAL");

        builder.HasIndex(i => i.SaleId);
    }
}

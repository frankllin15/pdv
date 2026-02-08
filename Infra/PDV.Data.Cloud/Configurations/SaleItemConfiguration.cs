using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;

namespace PDV.Data.Cloud.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.ProductDescription)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity)
            .HasColumnType("decimal(18,3)")
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.Discount)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Total)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(i => i.SaleId);
        
        builder.ToTable(tb => tb.HasTrigger("SalesItems_insert_trigger"));
    }
}

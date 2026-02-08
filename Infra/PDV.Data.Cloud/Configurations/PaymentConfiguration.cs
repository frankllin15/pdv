using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;

namespace PDV.Data.Cloud.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Method)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.AuthorizationCode)
            .HasMaxLength(50);

        builder.Property(p => p.CardBrand)
            .HasMaxLength(30);

        builder.Property(p => p.PaymentDate)
            .IsRequired();

        builder.HasIndex(p => p.SaleId);
        
        builder.ToTable(tb => tb.HasTrigger("Payments_insert_trigger"));
    }
}

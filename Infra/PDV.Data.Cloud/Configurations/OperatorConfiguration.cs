using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;

namespace PDV.Data.Cloud.Configurations;

public class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("Operators");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.PinHash)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.IsActive)
            .HasDefaultValue(true);

        builder.Property(o => o.IsAdmin)
            .HasDefaultValue(false);

        builder.HasIndex(o => o.Code)
            .IsUnique();
    }
}

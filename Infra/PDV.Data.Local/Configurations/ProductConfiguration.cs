using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDV.Core.Entities;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Configurations;

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
            .HasColumnType("REAL")
            .IsRequired();

        builder.Property(p => p.UnitOfMeasure)
            .HasMaxLength(10)
            .HasDefaultValue("UN");

        builder.Property(p => p.StockQuantity)
            .HasColumnType("REAL");

        builder.Property(p => p.TaxCode)
            .HasMaxLength(20);

        builder.Property(p => p.TaxRate)
            .HasColumnType("REAL");

        builder.Property(p => p.Cfop)
            .HasMaxLength(10);

        builder.Property(p => p.Cest)
            .HasMaxLength(10);

        builder.Property(p => p.TaxOrigin)
            .HasDefaultValue(TaxOrigin.National);

        builder.Property(p => p.Cst)
            .HasMaxLength(10);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.SyncState)
            .HasDefaultValue(SyncState.Pending);

        // // Seed data with deterministic GUIDs
        // var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //
        // builder.HasData(
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
        //         Barcode = "7891000100103",
        //         Description = "Leite Integral 1L",
        //         ShortDescription = "Leite 1L",
        //         UnitPrice = 5.99m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 100m,
        //         TaxCode = "04011010",
        //         TaxRate = 12m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
        //         Barcode = "7891000200109",
        //         Description = "Arroz Tipo 1 5kg",
        //         ShortDescription = "Arroz 5kg",
        //         UnitPrice = 24.90m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 50m,
        //         TaxCode = "10063011",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
        //         Barcode = "7891000300106",
        //         Description = "Feijão Carioca 1kg",
        //         ShortDescription = "Feijão 1kg",
        //         UnitPrice = 8.49m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 80m,
        //         TaxCode = "07133319",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
        //         Barcode = "7891000400103",
        //         Description = "Açúcar Refinado 1kg",
        //         ShortDescription = "Açúcar 1kg",
        //         UnitPrice = 4.99m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 120m,
        //         TaxCode = "17019900",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
        //         Barcode = "7891000500100",
        //         Description = "Café Torrado 500g",
        //         ShortDescription = "Café 500g",
        //         UnitPrice = 15.90m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 60m,
        //         TaxCode = "09012100",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
        //         Barcode = "7891000600107",
        //         Description = "Óleo de Soja 900ml",
        //         ShortDescription = "Óleo 900ml",
        //         UnitPrice = 7.99m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 70m,
        //         TaxCode = "15079019",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
        //         Barcode = "7891000700104",
        //         Description = "Macarrão Espaguete 500g",
        //         ShortDescription = "Macarrão 500g",
        //         UnitPrice = 4.49m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 90m,
        //         TaxCode = "19021100",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
        //         Barcode = "7891000800101",
        //         Description = "Biscoito Cream Cracker 400g",
        //         ShortDescription = "Biscoito 400g",
        //         UnitPrice = 5.49m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 100m,
        //         TaxCode = "19053100",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111109"),
        //         Barcode = "7891000900108",
        //         Description = "Refrigerante Cola 2L",
        //         ShortDescription = "Refri 2L",
        //         UnitPrice = 8.99m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 60m,
        //         TaxCode = "22021000",
        //         TaxRate = 7m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     },
        //     new
        //     {
        //         Id = Guid.Parse("11111111-1111-1111-1111-111111111110"),
        //         Barcode = "7891001000100",
        //         Description = "Sabonete 90g",
        //         ShortDescription = "Sabonete",
        //         UnitPrice = 2.99m,
        //         UnitOfMeasure = "UN",
        //         StockQuantity = 150m,
        //         TaxCode = "34011190",
        //         TaxRate = 18m,
        //         IsActive = true,
        //         SyncState = SyncState.Pending,
        //         CreatedAt = seedDate
        //     }
        // );
    }
}

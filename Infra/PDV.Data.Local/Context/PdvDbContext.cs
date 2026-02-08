using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;

namespace PDV.Data.Local.Context;

public class PdvDbContext(DbContextOptions<PdvDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<FiscalTransaction> FiscalTransactions => Set<FiscalTransaction>();
    public DbSet<FiscalConfiguration> FiscalConfigurations => Set<FiscalConfiguration>();
    public DbSet<FiscalReprintLog> FiscalReprintLogs => Set<FiscalReprintLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PdvDbContext).Assembly);

        // Store all Guid properties as BLOB (16 bytes) instead of TEXT (36 bytes)
        // for better index performance with UUIDv7 sequential keys
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                {
                    property.SetColumnType("BLOB");
                }
            }
        }
    }
}

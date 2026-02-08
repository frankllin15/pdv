using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;

namespace PDV.Data.Cloud.Context;

public class CloudDbContext(DbContextOptions<CloudDbContext> options) : DbContext(options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudDbContext).Assembly);
    }
}

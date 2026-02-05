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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PdvDbContext).Assembly);
    }
}

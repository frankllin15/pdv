using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;

namespace PDV.Data.Local.Context;

public class PdvDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    public PdvDbContext(DbContextOptions<PdvDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PdvDbContext).Assembly);
    }
}

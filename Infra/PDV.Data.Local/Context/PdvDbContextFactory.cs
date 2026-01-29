using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PDV.Data.Local.Context;

public class PdvDbContextFactory : IDesignTimeDbContextFactory<PdvDbContext>
{
    public PdvDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PdvDbContext>();
        optionsBuilder.UseSqlite("Data Source=pdv_migrations.db");
        return new PdvDbContext(optionsBuilder.Options);
    }
}

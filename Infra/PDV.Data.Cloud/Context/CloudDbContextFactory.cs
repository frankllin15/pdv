using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PDV.Data.Cloud.Context;

public class CloudDbContextFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args)
    {
        // Busca o appsettings.json do projeto API
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Presentation", "PDV.API");

        // Se executado da raiz da solução, ajusta o caminho
        if (!Directory.Exists(basePath))
        {
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "Presentation", "PDV.API");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<CloudDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new CloudDbContext(optionsBuilder.Options);
    }
}

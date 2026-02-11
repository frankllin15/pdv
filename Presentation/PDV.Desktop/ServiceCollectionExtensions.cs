using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Data.Local.Context;
using PDV.Data.Local.Handlers;
using PDV.Data.Local.Queries;
using PDV.Data.Local.Repositories;
using PDV.Desktop.I18n;
using PDV.Desktop.Services;
using PDV.Desktop.ViewModels;
using PDV.Fiscal.Providers;
using PDV.Fiscal.Services;
using PDV.Integration.Sync;

namespace PDV.Desktop;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdvDesktopServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        Console.WriteLine($"Database path: {connectionString.Replace("Data Source=", "")}");

        AddDataServices(services, connectionString);
        AddFiscalServices(services);
        AddPrintServices(services);
        AddSessionServices(services);
        AddViewModels(services);
        AddLogging(services);
        AddSyncServices(services, configuration, connectionString);

        services.AddSingleton(LocalizationManager.Instance);

        return services;
    }

    public static void InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PdvDbContext>();

        Dapper.SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

        context.Database.Migrate();

        // if (!context.Operators.Any())
        // {
        //     var defaultOperator = new Operator("Admin", "ADMIN", "1234", isAdmin: true);
        //     context.Operators.Add(defaultOperator);
        //     context.SaveChanges();
        //     Console.WriteLine("Default operator created: ADMIN / 1234");
        // }
    }

    public static void StartBackgroundSync(this IServiceProvider serviceProvider)
    {
        var backgroundSync = serviceProvider.GetRequiredService<BackgroundSyncService>();
        backgroundSync.StatusChanged += (sender, e) =>
        {
            Console.WriteLine($"[Sync] {e.Status}: {e.Message}");
        };
        backgroundSync.Start();
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var template = configuration.GetConnectionString("LocalDatabase")
            ?? "Data Source={LocalAppData}\\PDV\\pdv_local.db";

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var connectionString = template.Replace("{LocalAppData}", localAppData);

        var dbPath = connectionString.Replace("Data Source=", "");
        var dbFolder = Path.GetDirectoryName(dbPath);
        if (dbFolder is not null)
            Directory.CreateDirectory(dbFolder);

        return connectionString;
    }

    private static void AddDataServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PdvDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IProductQuery>(_ => new ProductQuery(connectionString));
        services.AddScoped<ISaleQuery>(_ => new SaleQuery(connectionString));
        services.AddScoped<IOperatorQuery>(_ => new OperatorQuery(connectionString));
    }

    private static void AddFiscalServices(IServiceCollection services)
    {
        services.AddScoped<IFiscalTransactionRepository, FiscalTransactionRepository>();
        services.AddScoped<IFiscalConfigurationRepository, FiscalConfigurationRepository>();
        services.AddScoped<IFiscalReprintLogRepository, FiscalReprintLogRepository>();

        services.AddSingleton<XmlBuilderService>();
        services.AddSingleton<DanfeService>();
        services.AddSingleton<IFiscalProvider, MockProvider>(sp =>
            new MockProvider(sp.GetRequiredService<ILogger<MockProvider>>()));
        services.AddScoped<IFiscalManager, FiscalManager>();
    }

    private static void AddPrintServices(IServiceCollection services)
    {
        services.AddSingleton<ThermalPrinterService>();
        services.AddSingleton<IReceiptPrinterService>(sp => sp.GetRequiredService<ThermalPrinterService>());
        services.AddSingleton<IThermalPrinterService>(sp => sp.GetRequiredService<ThermalPrinterService>());
    }

    private static void AddSessionServices(IServiceCollection services)
    {
        services.AddSingleton<IOperatorSessionService>(sp => new OperatorSessionService(sp));
    }

    private static void AddViewModels(IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CheckoutViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<SalesHistoryViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<FiscalConfigViewModel>();
        services.AddTransient<FiscalHistoryViewModel>();
    }

    private static void AddLogging(IServiceCollection services)
    {
        services.AddLogging();
    }

    private static void AddSyncServices(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        var apiUrl = configuration["Sync:ApiUrl"] ?? "http://localhost:5233";

        services.AddHttpClient();
        services.AddSingleton<ISyncService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<ILogger<SyncService>>();
            return new SyncService(connectionString, apiUrl, logger, httpClient);
        });
        services.AddSingleton<IApiAuthService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var syncService = sp.GetRequiredService<ISyncService>();
            return new ApiAuthService(httpClient, syncService, apiUrl);
        });
        services.AddSingleton<BackgroundSyncService>(sp =>
        {
            var syncService = sp.GetRequiredService<ISyncService>();
            var authService = sp.GetRequiredService<IApiAuthService>();
            return new BackgroundSyncService(syncService, authService);
        });
    }
}

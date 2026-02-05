using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
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
using PDV.Desktop.Services;
using PDV.Desktop.ViewModels;
using PDV.Desktop.Views;
using PDV.Fiscal.Providers;
using PDV.Fiscal.Services;
using PDV.Integration.Sync;

namespace PDV.Desktop;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Console.WriteLine("Configuring services...");
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            Console.WriteLine("Initializing database...");
            InitializeDatabase();

            Console.WriteLine("Starting background sync service...");
            StartBackgroundSync();

            Console.WriteLine("Creating main window...");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };
                Console.WriteLine("Main window created successfully!");
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in OnFrameworkInitializationCompleted: {ex}");
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database path
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbFolder = Path.Combine(localAppData, "PDV");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "pdv_local.db");
        var connectionString = $"Data Source={dbPath}";
        
        Console.WriteLine($"Database path: {dbPath}");

        // DbContext
        services.AddDbContext<PdvDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Queries (Dapper)
        services.AddScoped<IProductQuery>(_ => new ProductQuery(connectionString));
        services.AddScoped<ISaleQuery>(_ => new SaleQuery(connectionString));
        services.AddScoped<IOperatorQuery>(_ => new OperatorQuery(connectionString));

        // Fiscal Repositories
        services.AddScoped<IFiscalTransactionRepository, FiscalTransactionRepository>();
        services.AddScoped<IFiscalConfigurationRepository, FiscalConfigurationRepository>();
        services.AddScoped<IFiscalReprintLogRepository, FiscalReprintLogRepository>();

        // Fiscal Services
        services.AddSingleton<XmlBuilderService>();
        services.AddSingleton<DanfeService>();
        services.AddSingleton<IFiscalProvider, MockProvider>(sp =>
            new MockProvider(sp.GetRequiredService<ILogger<MockProvider>>()));
        services.AddScoped<IFiscalManager, FiscalManager>();

        // Print Services
        services.AddSingleton<IReceiptPrinterService, ReceiptPrinterService>();

        // Session Services
        services.AddSingleton<IOperatorSessionService>(sp => new OperatorSessionService(sp));

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CheckoutViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<SalesHistoryViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<FiscalConfigViewModel>();
        services.AddTransient<FiscalHistoryViewModel>();

        // Logging
        services.AddLogging();

        // Sync Services
        var apiUrl = "http://localhost:5233"; // URL da API
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

    private static void InitializeDatabase()
    {
        using var scope = Services!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PdvDbContext>();

        Dapper.SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

        // Apply pending migrations
        context.Database.Migrate();

        // Seed default operator if none exists
        if (!context.Operators.Any())
        {
            var defaultOperator = new Operator("Admin", "ADMIN", "1234", isAdmin: true);
            context.Operators.Add(defaultOperator);
            context.SaveChanges();
            Console.WriteLine("Default operator created: ADMIN / 1234");
        }
    }

    private static void StartBackgroundSync()
    {
        var backgroundSync = Services!.GetRequiredService<BackgroundSyncService>();
        backgroundSync.StatusChanged += (sender, e) =>
        {
            Console.WriteLine($"[Sync] {e.Status}: {e.Message}");
        };
        backgroundSync.Start();
    }
}

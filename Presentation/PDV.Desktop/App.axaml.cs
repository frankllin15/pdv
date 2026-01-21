using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local;
using PDV.Data.Local.Context;
using PDV.Data.Local.Queries;
using PDV.Data.Local.Repositories;
using PDV.Desktop.ViewModels;
using PDV.Desktop.Views;

namespace PDV.Desktop;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize database
        await InitializeDatabaseAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database path
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbFolder = Path.Combine(localAppData, "PDV");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "pdv_local.db");
        var connectionString = $"Data Source={dbPath}";

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

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<CheckoutViewModel>();
    }

    private static async Task InitializeDatabaseAsync()
    {
        using var scope = Services!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PdvDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed data
        await SeedData.InitializeAsync(context);
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PDV.Desktop.I18n;
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

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            LocalizationManager.Instance.SetCulture("pt-BR");
            Console.WriteLine("Configuring services...");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            services.AddPdvDesktopServices(configuration);
            Services = services.BuildServiceProvider();

            Console.WriteLine("Initializing database...");
            Services.InitializeDatabase();

            Console.WriteLine("Starting background sync service...");
            Services.StartBackgroundSync();

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
}

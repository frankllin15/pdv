using Avalonia;
using Serilog;
using Velopack;

namespace PDV.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        _ = CheckForUpdatesAsync();

        try
        {
            Console.WriteLine("Starting PDV Desktop...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex}");
            Console.ReadLine();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static async Task CheckForUpdatesAsync()
    {
        try
        {
            var mgr = new UpdateManager("https://github.com/frankllin15/pdv/releases/latest");

            if (!mgr.IsInstalled)
            {
                Log.Debug("Velopack: running in development mode, skipping update check");
                return;
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion is null)
            {
                Log.Information("Velopack: no updates available");
                return;
            }

            Log.Information("Velopack: downloading update {Version}", newVersion.TargetFullRelease.Version);
            await mgr.DownloadUpdatesAsync(newVersion);
            Log.Information("Velopack: update downloaded and ready to install on next restart");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Velopack: update check failed");
        }
    }
}

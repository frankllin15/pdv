using PDV.Integration.Sync;
using Polly;
using Polly.Retry;
using Serilog;

namespace PDV.Desktop.Services;

public class BackgroundSyncService : IDisposable
{
    private readonly ISyncService _syncService;
    private readonly IApiAuthService? _authService;
    private readonly Timer _timer;
    private readonly TimeSpan _syncInterval;
    private readonly AsyncRetryPolicy _retryPolicy;
    private bool _isRunning;
    private bool _disposed;

    public event EventHandler<SyncStatusEventArgs>? StatusChanged;
    public bool IsOnline { get; private set; }
    public DateTime? LastSyncTime { get; private set; }
    public SyncResult? LastSyncResult { get; private set; }

    public BackgroundSyncService(ISyncService syncService, IApiAuthService? authService = null, TimeSpan? syncInterval = null)
    {
        _syncService = syncService;
        _authService = authService;
        _syncInterval = syncInterval ?? TimeSpan.FromMinutes(5);

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Log.Warning(exception,
                        "Sync retry {RetryCount} after {Delay}s",
                        retryCount, timeSpan.TotalSeconds);
                });

        _syncService.ProgressChanged += OnSyncProgressChanged;

        _timer = new Timer(async _ => await TrySyncAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _timer.Change(TimeSpan.Zero, _syncInterval);

        Log.Information("Background sync service started with interval {Interval}",
            _syncInterval);

        OnStatusChanged(SyncStatus.Idle, "Sync service started");
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);

        Log.Information("Background sync service stopped");
        OnStatusChanged(SyncStatus.Stopped, "Sync service stopped");
    }

    public async Task SyncNowAsync()
    {
        await TrySyncAsync();
    }

    private async Task TrySyncAsync()
    {
        if (!_isRunning) return;

        try
        {
            OnStatusChanged(SyncStatus.Checking, "Checking server availability...");

            IsOnline = await _syncService.IsServerAvailableAsync();

            if (!IsOnline)
            {
                OnStatusChanged(SyncStatus.Offline, "Server not available - working offline");
                return;
            }

            // Ensure we have a valid auth token before syncing
            if (_authService != null)
            {
                OnStatusChanged(SyncStatus.Checking, "Authenticating...");

                if (!_authService.IsAuthenticated)
                {
                    var authenticated = await _authService.AuthenticateAsync();
                    if (!authenticated)
                    {
                        OnStatusChanged(SyncStatus.Error, "Authentication failed");
                        return;
                    }
                }
                else if (_authService.TokenExpiresAt < DateTime.UtcNow.AddMinutes(5))
                {
                    // Token expiring soon, refresh it
                    await _authService.RefreshTokenAsync();
                }
            }

            OnStatusChanged(SyncStatus.Syncing, "Synchronizing...");

            LastSyncResult = await _retryPolicy.ExecuteAsync(async () =>
                await _syncService.SynchronizeAsync());

            LastSyncTime = DateTime.Now;

            if (LastSyncResult.Success)
            {
                OnStatusChanged(SyncStatus.Completed,
                    $"Sync completed: ↓{LastSyncResult.TotalChangesDownloaded} ↑{LastSyncResult.TotalChangesUploaded}");
            }
            else
            {
                OnStatusChanged(SyncStatus.Error,
                    $"Sync failed: {LastSyncResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Background sync failed");
            OnStatusChanged(SyncStatus.Error, $"Sync error: {ex.Message}");
        }
    }

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        OnStatusChanged(SyncStatus.Syncing, e.Message);
    }

    private void OnStatusChanged(SyncStatus status, string message)
    {
        StatusChanged?.Invoke(this, new SyncStatusEventArgs
        {
            Status = status,
            Message = message,
            Timestamp = DateTime.Now,
            LastSyncResult = LastSyncResult
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Stop();
        _timer.Dispose();
        _syncService.ProgressChanged -= OnSyncProgressChanged;

        GC.SuppressFinalize(this);
    }
}

public class SyncStatusEventArgs : EventArgs
{
    public SyncStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public SyncResult? LastSyncResult { get; init; }
}

public enum SyncStatus
{
    Idle,
    Checking,
    Syncing,
    Completed,
    Offline,
    Error,
    Stopped
}

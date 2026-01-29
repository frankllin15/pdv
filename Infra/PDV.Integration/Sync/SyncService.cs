using System.Diagnostics;
using System.Net.Http.Headers;
using Dotmim.Sync;
using Dotmim.Sync.Sqlite;
using Dotmim.Sync.Web.Client;
using Microsoft.Extensions.Logging;

namespace PDV.Integration.Sync;

public class SyncService : ISyncService
{
    private readonly string _localConnectionString;
    private readonly string _serverUrl;
    private readonly ILogger<SyncService> _logger;
    private readonly HttpClient _httpClient;
    private string? _authToken;

    public event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    public SyncService(
        string localConnectionString,
        string serverUrl,
        ILogger<SyncService> logger,
        HttpClient httpClient)
    {
        _localConnectionString = localConnectionString;
        _serverUrl = serverUrl.TrimEnd('/') + "/api/sync";
        _logger = logger;
        _httpClient = httpClient;
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<SyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            OnProgressChanged("", SyncStage.Starting, 0, "Starting synchronization...");
            _logger.LogInformation("Starting sync with server {ServerUrl}", _serverUrl);

            // Configure providers
            var serverProvider = new WebRemoteOrchestrator(_serverUrl, client: _httpClient);
            var clientProvider = new SqliteSyncProvider(_localConnectionString);

            // Create sync agent
            var syncAgent = new SyncAgent(clientProvider, serverProvider);

            // Subscribe to progress events
            syncAgent.LocalOrchestrator.OnTableChangesApplied(args =>
            {
                OnProgressChanged(
                    args.TableChangesApplied.TableName,
                    SyncStage.Applying,
                    50,
                    $"Applied {args.TableChangesApplied.Applied} changes to {args.TableChangesApplied.TableName}");
            });

            // Execute sync
            var result = await syncAgent.SynchronizeAsync();

            stopwatch.Stop();

            _logger.LogInformation(
                "Sync completed. Downloaded: {Downloaded}, Uploaded: {Uploaded}, Duration: {Duration}ms",
                result.TotalChangesDownloadedFromServer,
                result.TotalChangesUploadedToServer,
                stopwatch.ElapsedMilliseconds);

            OnProgressChanged("", SyncStage.Completed, 100, "Synchronization completed");

            return new SyncResult
            {
                Success = true,
                TotalChangesDownloaded = result.TotalChangesDownloadedFromServer,
                TotalChangesUploaded = result.TotalChangesUploadedToServer,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Sync failed after {Duration}ms", stopwatch.ElapsedMilliseconds);

            OnProgressChanged("", SyncStage.Failed, 0, $"Sync failed: {ex.Message}");

            return new SyncResult
            {
                Success = false,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                _serverUrl.Replace("/api/sync", "/"),
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Server availability check failed");
            return false;
        }
    }

    private void OnProgressChanged(string tableName, SyncStage stage, int progress, string message)
    {
        ProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            TableName = tableName,
            Stage = stage,
            Progress = progress,
            Message = message
        });
    }
}

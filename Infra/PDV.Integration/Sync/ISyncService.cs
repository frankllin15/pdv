namespace PDV.Integration.Sync;

public interface ISyncService
{
    Task<SyncResult> SynchronizeAsync(CancellationToken cancellationToken = default);
    Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default);
    void SetAuthToken(string token);
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
}

public record SyncResult
{
    public bool Success { get; init; }
    public int TotalChangesDownloaded { get; init; }
    public int TotalChangesUploaded { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}

public class SyncProgressEventArgs : EventArgs
{
    public string TableName { get; init; } = string.Empty;
    public SyncStage Stage { get; init; }
    public int Progress { get; init; }
    public string Message { get; init; } = string.Empty;
}

public enum SyncStage
{
    Starting,
    Downloading,
    Uploading,
    Applying,
    Completed,
    Failed
}

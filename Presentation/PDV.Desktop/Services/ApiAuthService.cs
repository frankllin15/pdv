using System.Net.Http.Json;
using PDV.Integration.Sync;
using Serilog;

namespace PDV.Desktop.Services;

public interface IApiAuthService
{
    Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);
    Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);
    bool IsAuthenticated { get; }
    DateTime? TokenExpiresAt { get; }
}

public class ApiAuthService : IApiAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ISyncService _syncService;
    private readonly string _apiUrl;
    private string? _currentToken;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_currentToken) && TokenExpiresAt > DateTime.UtcNow;
    public DateTime? TokenExpiresAt { get; private set; }

    public ApiAuthService(HttpClient httpClient, ISyncService syncService, string apiUrl)
    {
        _httpClient = httpClient;
        _syncService = syncService;
        _apiUrl = apiUrl.TrimEnd('/');
    }

    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var storeId = Environment.MachineName;

            var request = new LoginRequest
            {
                Username = "admin",
                Password = "admin123",
                StoreId = storeId
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiUrl}/api/auth/login",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);

                if (loginResponse != null)
                {
                    _currentToken = loginResponse.Token;
                    TokenExpiresAt = loginResponse.ExpiresAt;

                    _syncService.SetAuthToken(_currentToken);

                    Log.Information("Successfully authenticated with API. Token expires at {ExpiresAt}",
                        TokenExpiresAt);

                    return true;
                }
            }

            Log.Warning("Authentication failed. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to authenticate with API");
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentToken))
        {
            return await AuthenticateAsync(cancellationToken);
        }

        try
        {
            var request = new RefreshRequest { Token = _currentToken };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiUrl}/api/auth/refresh",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);

                if (loginResponse != null)
                {
                    _currentToken = loginResponse.Token;
                    TokenExpiresAt = loginResponse.ExpiresAt;

                    _syncService.SetAuthToken(_currentToken);

                    Log.Information("Token refreshed. New expiration: {ExpiresAt}", TokenExpiresAt);
                    return true;
                }
            }

            Log.Warning("Token refresh failed. Re-authenticating...");
            return await AuthenticateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh token");
            return await AuthenticateAsync(cancellationToken);
        }
    }
}

internal record LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? StoreId { get; init; }
}

internal record RefreshRequest
{
    public string Token { get; init; } = string.Empty;
}

internal record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? StoreId { get; init; }
}

using Microsoft.Extensions.DependencyInjection;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Services;
using PDV.Shared.DTOs;

namespace PDV.Desktop.Services;

public class CashSessionService(IServiceProvider serviceProvider) : ICashSessionService
{
    public CashSessionDto? CurrentSession { get; private set; }
    public bool HasOpenSession => CurrentSession != null;

    public event Action<CashSessionDto?>? SessionChanged;

    public async Task LoadCurrentSessionAsync(string terminalId, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var cashSessionQuery = scope.ServiceProvider.GetRequiredService<ICashSessionQuery>();

        var session = await cashSessionQuery.GetOpenSessionByTerminalAsync(terminalId, cancellationToken);
        CurrentSession = session;
        SessionChanged?.Invoke(CurrentSession);
    }

    public void SetCurrentSession(CashSessionDto? session)
    {
        CurrentSession = session;
        SessionChanged?.Invoke(CurrentSession);
    }

    public void ClearSession()
    {
        CurrentSession = null;
        SessionChanged?.Invoke(null);
    }
}

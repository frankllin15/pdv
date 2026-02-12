using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Services;

public interface ICashSessionService
{
    CashSessionDto? CurrentSession { get; }
    bool HasOpenSession { get; }
    event Action<CashSessionDto?>? SessionChanged;

    Task LoadCurrentSessionAsync(string terminalId, CancellationToken cancellationToken = default);
    void SetCurrentSession(CashSessionDto? session);
    void ClearSession();
}

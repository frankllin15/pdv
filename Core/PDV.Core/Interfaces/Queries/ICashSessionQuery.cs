using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Queries;

public interface ICashSessionQuery
{
    Task<CashSessionDto?> GetOpenSessionByTerminalAsync(string terminalId, CancellationToken cancellationToken = default);
    Task<CashBalanceResultDto> CalculateSessionBalanceAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashTransactionDto>> GetTransactionsBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

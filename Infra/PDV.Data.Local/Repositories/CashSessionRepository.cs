using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Repositories;

public class CashSessionRepository(PdvDbContext context) : ICashSessionRepository
{
    public async Task<CashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.CashSessions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<CashSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.CashSessions.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CashSession entity, CancellationToken cancellationToken = default)
    {
        await context.CashSessions.AddAsync(entity, cancellationToken);
    }

    public void Update(CashSession entity)
    {
        context.CashSessions.Update(entity);
    }

    public void Remove(CashSession entity)
    {
        context.CashSessions.Remove(entity);
    }

    public async Task<CashSession?> GetOpenSessionByTerminalAsync(string terminalId, CancellationToken cancellationToken = default)
    {
        return await context.CashSessions
            .FirstOrDefaultAsync(s => s.TerminalId == terminalId && s.Status == SessionStatus.Open, cancellationToken);
    }

    public async Task<CashSession?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.CashSessions
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}

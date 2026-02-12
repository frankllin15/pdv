using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;

namespace PDV.Data.Local.Repositories;

public class CashTransactionRepository(PdvDbContext context) : ICashTransactionRepository
{
    public async Task<CashTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.CashTransactions.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<CashTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.CashTransactions.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CashTransaction entity, CancellationToken cancellationToken = default)
    {
        await context.CashTransactions.AddAsync(entity, cancellationToken);
    }

    public void Update(CashTransaction entity)
    {
        context.CashTransactions.Update(entity);
    }

    public void Remove(CashTransaction entity)
    {
        context.CashTransactions.Remove(entity);
    }

    public async Task<IEnumerable<CashTransaction>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await context.CashTransactions
            .Where(t => t.CashSessionId == sessionId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }
}

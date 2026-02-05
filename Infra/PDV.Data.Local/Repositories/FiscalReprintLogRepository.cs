using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;

namespace PDV.Data.Local.Repositories;

public class FiscalReprintLogRepository : IFiscalReprintLogRepository
{
    private readonly PdvDbContext _context;

    public FiscalReprintLogRepository(PdvDbContext context)
    {
        _context = context;
    }

    public async Task<FiscalReprintLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalReprintLogs
            .Include(f => f.FiscalTransaction)
            .Include(f => f.Operator)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<FiscalReprintLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FiscalReprintLogs
            .Include(f => f.FiscalTransaction)
            .Include(f => f.Operator)
            .OrderByDescending(f => f.ReprintedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FiscalReprintLog entity, CancellationToken cancellationToken = default)
    {
        await _context.FiscalReprintLogs.AddAsync(entity, cancellationToken);
    }

    public void Update(FiscalReprintLog entity)
    {
        _context.FiscalReprintLogs.Update(entity);
    }

    public void Remove(FiscalReprintLog entity)
    {
        _context.FiscalReprintLogs.Remove(entity);
    }

    public async Task<IEnumerable<FiscalReprintLog>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalReprintLogs
            .Include(f => f.Operator)
            .Where(f => f.FiscalTransactionId == transactionId)
            .OrderByDescending(f => f.ReprintedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetReprintCountAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalReprintLogs
            .CountAsync(f => f.FiscalTransactionId == transactionId, cancellationToken);
    }

    public async Task<IEnumerable<FiscalReprintLog>> GetByOperatorIdAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalReprintLogs
            .Include(f => f.FiscalTransaction)
            .Where(f => f.OperatorId == operatorId)
            .OrderByDescending(f => f.ReprintedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FiscalReprintLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalReprintLogs
            .Include(f => f.FiscalTransaction)
            .Include(f => f.Operator)
            .Where(f => f.ReprintedAt >= startDate && f.ReprintedAt <= endDate)
            .OrderByDescending(f => f.ReprintedAt)
            .ToListAsync(cancellationToken);
    }
}

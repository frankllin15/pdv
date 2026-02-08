using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Repositories;

public class FiscalTransactionRepository : IFiscalTransactionRepository
{
    private readonly PdvDbContext _context;

    public FiscalTransactionRepository(PdvDbContext context)
    {
        _context = context;
    }

    public async Task<FiscalTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<FiscalTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FiscalTransaction entity, CancellationToken cancellationToken = default)
    {
        await _context.FiscalTransactions.AddAsync(entity, cancellationToken);
    }

    public void Update(FiscalTransaction entity)
    {
        _context.FiscalTransactions.Update(entity);
    }

    public void Remove(FiscalTransaction entity)
    {
        _context.FiscalTransactions.Remove(entity);
    }

    public async Task<IEnumerable<FiscalTransaction>> GetPendingContingencyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions
            .Include(f => f.Sale)
            .Where(f => f.IsContingency && f.Status == FiscalStatus.Contingency)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FiscalTransaction?> GetBySaleIdAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions
            .Include(f => f.Sale)
            .FirstOrDefaultAsync(f => f.SaleId == saleId, cancellationToken);
    }

    public async Task<FiscalTransaction?> GetByAccessKeyAsync(string accessKey, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions
            .Include(f => f.Sale)
            .FirstOrDefaultAsync(f => f.AccessKey == accessKey, cancellationToken);
    }

    public async Task<int> GetNextNumberAsync(int series, CancellationToken cancellationToken = default)
    {
        var maxNumber = await _context.FiscalTransactions
            .Where(f => f.Series == series)
            .MaxAsync(f => (int?)f.Number, cancellationToken) ?? 0;
        return maxNumber + 1;
    }

    public async Task<IEnumerable<FiscalTransaction>> GetByStatusAsync(FiscalStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions
            .Include(f => f.Sale)
            .Where(f => f.Status == status)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FiscalTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalTransactions
            .Include(f => f.Sale)
            .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<FiscalTransaction> Items, int TotalCount)> GetPagedAsync(
        DateTime startDate, DateTime endDate, FiscalStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.FiscalTransactions
            .Include(f => f.Sale)
            .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

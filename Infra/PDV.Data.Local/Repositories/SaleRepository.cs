using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly PdvDbContext _context;

    public SaleRepository(PdvDbContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Sale>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sales.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Sale entity, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(entity, cancellationToken);
    }

    public void Update(Sale entity)
    {
        _context.Sales.Update(entity);
    }

    public void Remove(Sale entity)
    {
        _context.Sales.Remove(entity);
    }

    public async Task<Sale?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale?> GetByIdWithItemsAndPaymentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<int> GetNextSaleNumberAsync(CancellationToken cancellationToken = default)
    {
        var maxNumber = await _context.Sales
            .MaxAsync(s => (int?)s.SaleNumber, cancellationToken) ?? 0;
        return maxNumber + 1;
    }

    public async Task<IEnumerable<Sale>> GetByStatusAsync(SaleStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Sale>> GetPendingSyncAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Where(s => s.SyncState == SyncState.Pending)
            .ToListAsync(cancellationToken);
    }
}

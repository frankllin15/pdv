using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;

namespace PDV.Data.Local.Repositories;

public class FiscalConfigurationRepository : IFiscalConfigurationRepository
{
    private readonly PdvDbContext _context;

    public FiscalConfigurationRepository(PdvDbContext context)
    {
        _context = context;
    }

    public async Task<FiscalConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalConfigurations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<FiscalConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FiscalConfigurations.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FiscalConfiguration entity, CancellationToken cancellationToken = default)
    {
        await _context.FiscalConfigurations.AddAsync(entity, cancellationToken);
    }

    public void Update(FiscalConfiguration entity)
    {
        _context.FiscalConfigurations.Update(entity);
    }

    public void Remove(FiscalConfiguration entity)
    {
        _context.FiscalConfigurations.Remove(entity);
    }

    public async Task<FiscalConfiguration?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FiscalConfigurations
            .FirstOrDefaultAsync(f => f.IsActive, cancellationToken);
    }

    public async Task<FiscalConfiguration?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default)
    {
        var normalizedTaxId = taxId.Replace(".", "").Replace("/", "").Replace("-", "");
        return await _context.FiscalConfigurations
            .FirstOrDefaultAsync(f => f.TaxId == normalizedTaxId, cancellationToken);
    }
}

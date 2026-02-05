using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Repositories;

public interface IFiscalConfigurationRepository : IRepository<FiscalConfiguration>
{
    Task<FiscalConfiguration?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<FiscalConfiguration?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default);
}

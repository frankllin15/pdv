using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}

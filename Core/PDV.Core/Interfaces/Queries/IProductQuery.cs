using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Queries;

public interface IProductQuery
{
    Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> SearchAsync(string searchTerm, int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

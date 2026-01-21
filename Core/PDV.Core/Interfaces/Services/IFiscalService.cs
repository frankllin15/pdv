using PDV.Core.Entities;

namespace PDV.Core.Interfaces.Services;

public interface IFiscalService
{
    Task<string> GenerateReceiptAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<bool> ValidateCustomerDocumentAsync(string document, CancellationToken cancellationToken = default);
}

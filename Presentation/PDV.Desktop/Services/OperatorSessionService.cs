using Microsoft.Extensions.DependencyInjection;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using PDV.Shared.DTOs;

namespace PDV.Desktop.Services;

public class OperatorSessionService(IServiceProvider serviceProvider) : IOperatorSessionService
{
    public OperatorDto? CurrentOperator { get; private set; }
    public bool IsLoggedIn => CurrentOperator != null;

    public event Action<OperatorDto?>? OperatorChanged;

    public async Task<bool> LoginAsync(string code, string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(pin))
            return false;

        using var scope = serviceProvider.CreateScope();
        var operatorQuery = scope.ServiceProvider.GetRequiredService<IOperatorQuery>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pinHash = HashPin(pin);
        var isValid = await operatorQuery.ValidatePinAsync(code, pinHash, cancellationToken);

        if (!isValid)
            return false;

        var operatorDto = await operatorQuery.GetByCodeAsync(code, cancellationToken);
        if (operatorDto == null)
            return false;

        // Record login in repository
        var operatorEntity = await unitOfWork.Operators.GetByCodeAsync(code, cancellationToken);
        if (operatorEntity != null)
        {
            operatorEntity.RecordLogin();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        CurrentOperator = operatorDto;
        OperatorChanged?.Invoke(CurrentOperator);

        return true;
    }

    public void Logout()
    {
        CurrentOperator = null;
        OperatorChanged?.Invoke(null);
    }

    private static string HashPin(string pin)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(pin);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

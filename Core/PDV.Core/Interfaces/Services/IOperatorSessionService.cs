using PDV.Shared.DTOs;

namespace PDV.Core.Interfaces.Services;

public interface IOperatorSessionService
{
    OperatorDto? CurrentOperator { get; }
    bool IsLoggedIn { get; }
    event Action<OperatorDto?>? OperatorChanged;

    Task<bool> LoginAsync(string code, string pin, CancellationToken cancellationToken = default);
    void Logout();
}

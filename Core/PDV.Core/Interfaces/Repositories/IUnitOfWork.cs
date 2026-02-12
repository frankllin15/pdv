namespace PDV.Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ISaleRepository Sales { get; }
    IOperatorRepository Operators { get; }
    ICashSessionRepository CashSessions { get; }
    ICashTransactionRepository CashTransactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

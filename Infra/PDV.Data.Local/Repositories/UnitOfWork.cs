using Microsoft.EntityFrameworkCore.Storage;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;

namespace PDV.Data.Local.Repositories;

public class UnitOfWork(PdvDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    private IProductRepository? _products;
    private ISaleRepository? _sales;
    private IOperatorRepository? _operators;
    private ICashSessionRepository? _cashSessions;
    private ICashTransactionRepository? _cashTransactions;

    public IProductRepository Products => _products ??= new ProductRepository(context);
    public ISaleRepository Sales => _sales ??= new SaleRepository(context);
    public IOperatorRepository Operators => _operators ??= new OperatorRepository(context);
    public ICashSessionRepository CashSessions => _cashSessions ??= new CashSessionRepository(context);
    public ICashTransactionRepository CashTransactions => _cashTransactions ??= new CashTransactionRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}

using Microsoft.EntityFrameworkCore;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Data.Local.Context;

namespace PDV.Data.Local.Repositories;

public class OperatorRepository(PdvDbContext context) : IOperatorRepository
{
    public async Task<Operator?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Operators.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Operator>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Operators.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Operator entity, CancellationToken cancellationToken = default)
    {
        await context.Operators.AddAsync(entity, cancellationToken);
    }

    public void Update(Operator entity)
    {
        context.Operators.Update(entity);
    }

    public void Remove(Operator entity)
    {
        context.Operators.Remove(entity);
    }

    public async Task<Operator?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await context.Operators
            .FirstOrDefaultAsync(o => o.Code == code.ToUpperInvariant() && o.IsActive, cancellationToken);
    }
}

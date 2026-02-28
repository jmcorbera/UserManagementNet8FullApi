using Microsoft.EntityFrameworkCore;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly MySqlDbContext _dbContext;

    public IdempotencyRepository(MySqlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyKey?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(i => i.Key == key, cancellationToken);
    }

    public async Task<bool> TryCreateAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.IdempotencyKeys.AddAsync(idempotencyKey, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    public async Task MarkAsCompletedAsync(Guid key, string result, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = await _dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(i => i.Key == key, cancellationToken);

        if (idempotencyKey != null)
        {
            idempotencyKey.SetResult(result);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid key, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = await _dbContext.IdempotencyKeys
            .FirstOrDefaultAsync(i => i.Key == key, cancellationToken);

        if (idempotencyKey != null)
        {
            idempotencyKey.SetFailed();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is MySqlConnector.MySqlException mysqlEx && mysqlEx.Number == 1062;
}

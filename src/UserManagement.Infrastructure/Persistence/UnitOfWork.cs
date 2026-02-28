using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MySqlDbContext _dbContext;

    public UnitOfWork(MySqlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;

namespace UserManagement.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly MySqlDbContext _dbContext;

    public OutboxMessageRepository(MySqlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .Where(o => o.ProcessedAt == null)
            .OrderBy(o => o.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (message != null)
        {
            message.MarkAsProcessed();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (message != null)
        {
            message.MarkAsFailed(error);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

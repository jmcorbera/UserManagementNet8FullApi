using UserManagement.Domain.Entities;

namespace UserManagement.Domain.Repositories;

public interface IOutboxMessageRepository
{
    Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
}

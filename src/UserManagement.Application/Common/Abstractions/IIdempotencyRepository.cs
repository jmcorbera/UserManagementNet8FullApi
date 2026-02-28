using UserManagement.Domain.Entities;

namespace UserManagement.Application.Common.Abstractions;

public interface IIdempotencyRepository
{
    Task<bool> TryCreateAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken = default);
    Task<IdempotencyKey?> GetByKeyAsync(Guid key, CancellationToken cancellationToken = default);
    Task MarkAsCompletedAsync(Guid key, string result, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid key, CancellationToken cancellationToken = default);
}

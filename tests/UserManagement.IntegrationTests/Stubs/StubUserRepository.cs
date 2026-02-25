using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubUserRepository : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> GetByCognitoSubAsync(string cognitoSub, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<User>());

    public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}

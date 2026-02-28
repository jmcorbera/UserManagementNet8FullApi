using UserManagement.Application.Common.Abstractions;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }
}

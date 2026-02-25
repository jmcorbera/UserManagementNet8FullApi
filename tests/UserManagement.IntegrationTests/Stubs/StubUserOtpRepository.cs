using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubUserOtpRepository : IUserOtpRepository
{
    public Task<UserOtp?> GetByEmailAndCodeAsync(Email email, string code, CancellationToken cancellationToken = default) =>
        Task.FromResult<UserOtp?>(null);

    public Task<UserOtp?> GetLatestByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult<UserOtp?>(null);

    public Task AddAsync(UserOtp otp, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateAsync(UserOtp otp, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

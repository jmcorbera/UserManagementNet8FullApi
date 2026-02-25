using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeUserOtpRepository : IUserOtpRepository
{
    private readonly List<UserOtp> _otps = new();

    public Task<UserOtp?> GetByEmailAndCodeAsync(Email email, string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_otps.FirstOrDefault(o => o.Email.ToString() == email.ToString() && o.Code == code));

    public Task<UserOtp?> GetLatestByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_otps.Where(o => o.Email.ToString() == email.ToString()).OrderByDescending(o => o.CreatedAt).FirstOrDefault());

    public Task AddAsync(UserOtp otp, CancellationToken cancellationToken = default)
    {
        _otps.Add(otp);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserOtp otp, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

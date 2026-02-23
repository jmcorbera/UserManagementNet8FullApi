using UserManagement.Domain.Entities;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Repositories;

/// <summary>
/// Repositorio para operaciones sobre UserOtp.
/// </summary>
public interface IUserOtpRepository
{
    Task<UserOtp?> GetByEmailAndCodeAsync(Email email, string code, CancellationToken cancellationToken = default);
    Task<UserOtp?> GetLatestByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task AddAsync(UserOtp otp, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserOtp otp, CancellationToken cancellationToken = default);
}

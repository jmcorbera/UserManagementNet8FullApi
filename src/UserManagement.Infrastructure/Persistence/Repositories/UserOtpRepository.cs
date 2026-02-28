using Microsoft.EntityFrameworkCore;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Infrastructure.Persistence.Repositories;

public sealed class UserOtpRepository : IUserOtpRepository
{
    private readonly MySqlDbContext _dbContext;

    public UserOtpRepository(MySqlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserOtp?> GetByEmailAndCodeAsync(Email email, string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserOtps
            .FirstOrDefaultAsync(o => o.Email == email && o.Code == code, cancellationToken);
    }

    public async Task<UserOtp?> GetLatestByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserOtps
            .Where(o => o.Email == email)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(UserOtp otp, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserOtps.AddAsync(otp, cancellationToken);
    }

    public async Task UpdateAsync(UserOtp otp, CancellationToken cancellationToken = default)
    {
        _dbContext.UserOtps.Update(otp);
    }
}

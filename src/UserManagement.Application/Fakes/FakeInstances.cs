
using UserManagement.Application.Common.Abstractions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

// WARNING: These are fake implementations for development purposes only
// Real implementation will be added later in infrastructure layer
namespace UserManagement.Application.Fakes.FakeInstances;

public class UserRepository : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetByCognitoSubAsync(string cognitoSub, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class UserOtpRepository : IUserOtpRepository
{
    public Task AddAsync(UserOtp userOtp, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserOtp?> GetByEmailAndCodeAsync(Email email, string code, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserOtp?> GetLatestByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(UserOtp userOtp, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class EmailSender : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class OtpGenerator : IOtpGenerator
{
    public string Generate()
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateAsync(int length = 6, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    DateTimeOffset IDateTimeProvider.UtcNow => UtcNow;
}

public class CognitoIdentityService : ICognitoIdentityService
{
    public Task<string> CreateUserAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetSubByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
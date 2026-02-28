
using UserManagement.Application.Common.Abstractions;

// WARNING: These are fake implementations for development purposes only
// Real implementation will be added later in infrastructure layer
namespace UserManagement.Application.Fakes.FakeInstances;

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
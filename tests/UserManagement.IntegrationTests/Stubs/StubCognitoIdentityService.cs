using UserManagement.Application.Common.Abstractions;

namespace UserManagement.IntegrationTests.Stubs;

public sealed class StubCognitoIdentityService : ICognitoIdentityService
{
    public Task<string> CreateUserAsync(string email, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult("stub-cognito-sub");

    public Task<string?> GetSubByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}

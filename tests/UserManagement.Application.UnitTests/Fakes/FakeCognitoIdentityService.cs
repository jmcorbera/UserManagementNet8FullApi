using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeCognitoIdentityService : ICognitoIdentityService
{
    public string? CreateUserSub { get; set; } = "cognito-sub-123";
    public string? GetSubByEmailResult { get; set; }

    public Task<string> CreateUserAsync(string email, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateUserSub ?? "cognito-sub-123");

    public Task<string?> GetSubByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(GetSubByEmailResult);
}

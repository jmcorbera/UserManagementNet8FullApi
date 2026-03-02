using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Infrastructure.Services;

public sealed class MockCognitoIdentityService : ICognitoIdentityService
{
    private readonly ILogger<MockCognitoIdentityService> _logger;
    private readonly ConcurrentDictionary<string, string> _usersByEmail = new();

    public MockCognitoIdentityService(ILogger<MockCognitoIdentityService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateUserAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        var cognitoSub = $"mock-cognito-{Guid.NewGuid()}";
        
        _usersByEmail.AddOrUpdate(email.ToLowerInvariant(), cognitoSub, (_, _) => cognitoSub);
        
        _logger.LogInformation(
            "Mock Cognito: Created user {Email} with name {Name} and sub {CognitoSub}",
            email,
            name,
            cognitoSub);

        return Task.FromResult(cognitoSub);
    }

    public Task<string?> GetSubByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var found = _usersByEmail.TryGetValue(email.ToLowerInvariant(), out var cognitoSub);
        
        _logger.LogInformation(
            "Mock Cognito: Lookup user by email {Email} - Found: {Found}, Sub: {CognitoSub}",
            email,
            found,
            cognitoSub);

        return Task.FromResult(cognitoSub);
    }
}

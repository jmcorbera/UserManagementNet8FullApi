namespace UserManagement.Application.Common.Abstractions;

/// <summary>
/// Port for creating/reading users in AWS Cognito. Implemented in Infrastructure.
/// </summary>
public interface ICognitoIdentityService
{
    /// <summary>
    /// Creates a user in Cognito for the given email and temporary password (or sends invite).
    /// Returns the Cognito sub (subject) identifier.
    /// </summary>
    Task<string> CreateUserAsync(string email, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Cognito sub for an existing user by email, or null if not found.
    /// </summary>
    Task<string?> GetSubByEmailAsync(string email, CancellationToken cancellationToken = default);
}

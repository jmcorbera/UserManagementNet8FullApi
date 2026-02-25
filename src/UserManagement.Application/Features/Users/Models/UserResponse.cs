using UserManagement.Domain.Enums;

namespace UserManagement.Application.Features.Users.Models;

/// <summary>
/// DTO for user in list/detail responses.
/// </summary>
public sealed class UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public UserStatus Status { get; init; }
    public string? CognitoSub { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public bool IsDeleted { get; init; }
}

using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Events;

/// <summary>
/// Domain Event que se publica cuando un usuario es verificado y activado (OTP validado, Cognito creado).
/// </summary>
public sealed record UserVerified(
    Guid UserId,
    Email Email,
    string CognitoSub,
    DateTimeOffset OccurredAt
) : IDomainEvent;

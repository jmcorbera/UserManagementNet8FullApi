using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Events;

/// <summary>
/// Domain Event que se publica cuando se solicita el registro de un usuario (OTP generado).
/// </summary>
public sealed record UserRegistrationRequested(
    Guid UserId,
    Email Email,
    string Name,
    string OtpCode,
    DateTimeOffset OccurredAt
) : IDomainEvent;

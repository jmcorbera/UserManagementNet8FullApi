using UserManagement.Domain.Common;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Entities;

/// <summary>
/// Entidad que representa un código OTP asociado a un usuario/email.
/// </summary>
public class UserOtp
{
    private UserOtp() { } // Para EF Core

    private UserOtp(Guid id, Email email, string code, DateTimeOffset expiresAt)
    {
        Id = id;
        Email = email;
        Code = code;
        ExpiresAt = expiresAt;
        Used = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }


    public Guid Id { get; private set; }
    public Email Email { get; private set; } = null!;
    public string Code { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool Used { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static UserOtp Create(Guid id, Email email, string code, TimeSpan validFor)
    {
        var now = DateTimeOffset.UtcNow;
        return new UserOtp(
            Guid.NewGuid(),
            email,
            code,
            now.Add(validFor)
        );
    }

    /// <summary>
    /// Marca el OTP como usado.
    /// </summary>
    public void MarkAsUsed()
    {
        if (Used)
        {
            throw new DomainException("OTP has already been used.");
        }

        Used = true;
    }

    /// <summary>
    /// Verifica si el OTP es válido (no usado y no expirado).
    /// </summary>
    public bool IsValid(DateTimeOffset now) => !Used && ExpiresAt > now;

    public bool IsValid() => IsValid(DateTimeOffset.UtcNow);
}

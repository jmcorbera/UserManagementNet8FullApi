using UserManagement.Domain.Common;
using UserManagement.Domain.Enums;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Entities;

/// <summary>
/// Aggregate Root que representa un usuario en el sistema.
/// </summary>
public class User : BaseAuditableEntity<Guid>
{
    private User() { } // Para EF Core

    private User(Guid id, Email email, string name, UserStatus status)
    {
        Id = id;
        Email = email;
        Name = name;
        Status = status;
        IsDeleted = false;
        SetCreated(null);
        SetLastModified(null);
    }

    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; }
    public string? CognitoSub { get; private set; }
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Activa el usuario (cambia estado a Active).
    /// </summary>
    public void Activate()
    {
        if (Status == UserStatus.Active)
        {
            return; // Ya está activo
        }

        Status = UserStatus.Active;
        SetLastModified(null);
    }

    /// <summary>
    /// Asigna el CognitoSub después de crear el usuario en Cognito.
    /// </summary>
    public void SetCognitoSub(string cognitoSub)
    {
        if (string.IsNullOrWhiteSpace(cognitoSub))
        {
            throw new DomainException($"CognitoSub cannot be null or empty: {cognitoSub}");
        }

        CognitoSub = cognitoSub;
        SetLastModified(null);
    }

    /// <summary>
    /// Realiza soft delete del usuario.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
        {
            return; // Ya está eliminado
        }

        IsDeleted = true;
        SetLastModified(null);
    }

    /// <summary>
    /// Actualiza el nombre del usuario.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name cannot be null or empty.");
        }

        Name = name;
        SetLastModified(null);
    }

    /// <summary>
    /// Factory method para crear un usuario pendiente de verificación.
    /// </summary>
    public static User CreatePending(Guid id, Email email, string name)
    {
        return new User(id, email, name, UserStatus.PendingVerification);
    }

    /// <summary>
    /// Factory method para crear un usuario desde Cognito (sync).
    /// </summary>
    public static User FromCognito(Guid id, Email email, string name, string cognitoSub)
    {
        var user = new User(id, email, name, UserStatus.Active);
        user.SetCognitoSub(cognitoSub);
        return user;
    }
}

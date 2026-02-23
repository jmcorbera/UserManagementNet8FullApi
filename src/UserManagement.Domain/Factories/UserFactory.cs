using UserManagement.Domain.Entities;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Domain.Factories;

/// <summary>
/// Factory para crear instancias de User según diferentes escenarios.
/// </summary>
public static class UserFactory
{
    /// <summary>
    /// Crea un usuario pendiente de verificación (registro con OTP).
    /// </summary>
    public static User CreatePending(Email email, string name)
    {
        return User.CreatePending(Guid.NewGuid(), email, name);
    }

    /// <summary>
    /// Crea un usuario desde Cognito (sync).
    /// </summary>
    public static User FromCognito(Email email, string name, string cognitoSub)
    {
        return User.FromCognito(Guid.NewGuid(), email, name, cognitoSub);
    }
}

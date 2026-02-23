namespace UserManagement.Domain.Enums;

/// <summary>
/// Estado del usuario en el sistema.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Usuario registrado pero pendiente de verificación (OTP).
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// Usuario activo y verificado.
    /// </summary>
    Active = 1
}

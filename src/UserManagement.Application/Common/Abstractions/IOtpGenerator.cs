namespace UserManagement.Application.Common.Abstractions;

/// <summary>
/// Port for generating OTP codes. Implemented in Application or Infrastructure.
/// </summary>
public interface IOtpGenerator
{
    /// <summary>
    /// Generates a new OTP code (e.g. 6-digit string).
    /// </summary>
    string Generate();
}

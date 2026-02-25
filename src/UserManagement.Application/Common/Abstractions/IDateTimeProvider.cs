namespace UserManagement.Application.Common.Abstractions;

/// <summary>
/// Port for current UTC time. Used for OTP expiration and validation.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

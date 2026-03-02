namespace UserManagement.Application.Common.Abstractions;

/// <summary>
/// Port for sending emails (e.g. OTP). Implemented in Infrastructure (e.g. AWS SES).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email to the given address using the specified template and data.
    /// </summary>
    Task SendAsync<T>(string toEmail, string templateName, T data, CancellationToken cancellationToken = default);
}

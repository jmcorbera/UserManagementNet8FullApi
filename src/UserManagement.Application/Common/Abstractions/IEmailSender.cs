namespace UserManagement.Application.Common.Abstractions;

/// <summary>
/// Port for sending emails (e.g. OTP). Implemented in Infrastructure (e.g. AWS SES).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email to the given address with the given subject and body.
    /// </summary>
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}

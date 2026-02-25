using UserManagement.Application.Common.Abstractions;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((toEmail, subject, body));
        return Task.CompletedTask;
    }
}

using System.Text.Json;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.Services;

public sealed class SesEmailSender : IEmailSender
{
    private readonly SesOptions _options;
    private readonly ILogger<SesEmailSender> _logger;
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;

    public SesEmailSender(
        IOptions<SesOptions> options,
        ILogger<SesEmailSender> logger,
        IAmazonSimpleEmailServiceV2 sesClient)
    {
        _options = options.Value;
        _logger = logger;
        _sesClient = sesClient;
    }

    public async Task SendAsync<T>(string toEmail, string templateName, T data, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendEmailRequest
            {
                FromEmailAddress = $"{_options.FromName} <{_options.FromEmail}>",
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                ReplyToAddresses = new List<string>
                {
                    _options.ReplyTo ?? _options.FromEmail
                },
                Content = new EmailContent
                {
                    Template = new Template
                    {
                        TemplateName = templateName,
                        TemplateData = JsonSerializer.Serialize(data)
                    }
                }
            };

            _logger.LogInformation(
                "Sending email via Amazon SES to {ToEmail} with template {TemplateName}",
                toEmail,
                templateName);

            var response = await _sesClient.SendEmailAsync(request, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {ToEmail}. MessageId: {MessageId}",
                toEmail,
                response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {ToEmail} with template {TemplateName}",
                toEmail,
                templateName);
            throw;
        }
    }
}

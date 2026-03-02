using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.Services;

public sealed class SnsMessagePublisher : IMessagePublisher
{
    private readonly SnsOptions _options;
    private readonly ILogger<SnsMessagePublisher> _logger;
    private readonly AmazonSimpleNotificationServiceClient  _snsClient;

    public SnsMessagePublisher(
        IOptions<SnsOptions> options,
        ILogger<SnsMessagePublisher> logger,
        AmazonSimpleNotificationServiceClient snsClient)
    {
        _options = options.Value;
        _logger = logger;
        _snsClient = snsClient;
    }

    public async Task PublishMessageAsync(string eventType, string eventContent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.TopicArn))
            {
                _logger.LogWarning("SNS TopicArn not configured. Skipping message publication.");
                return;
            }

            var request = new PublishRequest
            {
                TopicArn = _options.TopicArn,
                Message = eventContent,
                Subject = eventType,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["EventType"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = eventType
                    }
                }
            };

            _logger.LogInformation(
                "Publishing message to SNS topic {TopicArn} with event type {EventType}",
                _options.TopicArn,
                eventType);

            var response = await _snsClient.PublishAsync(request, cancellationToken);

            _logger.LogInformation(
                "Successfully published message to SNS. MessageId: {MessageId}",
                response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message to SNS topic {TopicArn} with event type {EventType}",
                _options.TopicArn,
                eventType);
            throw;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Repositories;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.BackgroundServices;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxProcessorOptions _options;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxProcessorOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var messagePublisher = scope.ServiceProvider.GetService<IMessagePublisher>();

        var messages = await repository.GetUnprocessedAsync(_options.BatchSize, cancellationToken);

        foreach (var message in messages)
        {
            if (message.NextRetryAt.HasValue && !message.IsReadyForRetry())
            {
                _logger.LogDebug(
                    "Skipping message {MessageId} - retry scheduled for {NextRetryAt}",
                    message.Id,
                    message.NextRetryAt);
                continue;
            }

            await ProcessSingleMessageAsync(message, repository, messagePublisher, cancellationToken);

         }
    }

    private async Task ProcessSingleMessageAsync(
        OutboxMessage message,
        IOutboxMessageRepository repository,
        IMessagePublisher? publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing outbox message {MessageId} of type {MessageType} (Attempt {RetryCount})",
                message.Id,
                message.Type,
                message.RetryCount + 1);

            if (publisher != null)
            {
                await publisher.PublishMessageAsync(message.Type, message.Content, cancellationToken);

                _logger.LogInformation(
                    "Domain Event Published to message bus: {EventType}",
                    message.Type);
            }
            else
            {
                _logger.LogInformation(
                    "Domain Event (No publisher configured): {EventType} - Content: {Content}",
                    message.Type,
                    TruncateContent(message.Content));
            }

            await repository.MarkAsProcessedAsync(message.Id, cancellationToken);

            _logger.LogInformation(
                "Successfully processed outbox message {MessageId}",
                message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);

            if (message.CanRetry(_options.MaxRetries))
            {
                message.IncrementRetry(_options.InitialRetryDelaySeconds, _options.RetryBackoffMultiplier);
                message.MarkAsFailed($"Retry {message.RetryCount}/{_options.MaxRetries}: {ex.Message}");

                _logger.LogWarning(
                    "Scheduled retry {RetryCount}/{MaxRetries} for message {MessageId} at {NextRetryAt}",
                    message.RetryCount,
                    _options.MaxRetries,
                    message.Id,
                    message.NextRetryAt);
            }
            else
            {
                message.MarkAsFailed($"Max retries exceeded: {ex.Message}");

                _logger.LogError(
                    "Message {MessageId} permanently failed after {RetryCount} retries",
                    message.Id,
                    message.RetryCount);
            }

            await repository.MarkAsFailedAsync(message.Id, message.Error ?? ex.Message, cancellationToken);
        }
    }

    private static string TruncateContent(string content, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        return content.Length <= maxLength ? content : content.Substring(0, maxLength) + "...";
    }

}

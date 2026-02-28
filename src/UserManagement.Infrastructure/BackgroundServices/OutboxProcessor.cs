using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Domain.Repositories;

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

        var messages = await repository.GetUnprocessedAsync(_options.BatchSize, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation(
                    "Processing outbox message {MessageId} of type {MessageType}",
                    message.Id,
                    message.Type);

                _logger.LogInformation(
                    "Domain Event Published: {EventType} - Content: {Content}",
                    message.Type,
                    message.Content);

                await repository.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Successfully processed outbox message {MessageId}",
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process outbox message {MessageId}",
                    message.Id);

                await repository.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    cancellationToken);
            }
        }
    }
}

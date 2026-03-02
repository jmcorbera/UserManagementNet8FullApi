using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Domain.Repositories;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.BackgroundServices;

public sealed class OutboxCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxCleanupJob> _logger;
    private readonly CleanupJobOptions _options;

    public OutboxCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<OutboxCleanupJob> logger,
        IOptions<CleanupJobOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Cleanup Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(_options.CleanupIntervalHours), stoppingToken);

            try
            {
                await CleanupOldMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old outbox messages");
            }
        }

        _logger.LogInformation("Outbox Cleanup Job stopped");
    }

    private async Task CleanupOldMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-_options.OutboxRetentionDays);

        await PerformCleanupAsync(repository, cutoffDate, cancellationToken);
    }

    private async Task PerformCleanupAsync(IOutboxMessageRepository repository, DateTimeOffset cutoffDate, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting cleanup of outbox messages older than {CutoffDate} ({RetentionDays} days)",
            cutoffDate,
            _options.OutboxRetentionDays);

        var deletedCount = await repository.DeleteOlderThanAsync(cutoffDate, cancellationToken);

        _logger.LogInformation(
            "Cleanup completed. Deleted {DeletedCount} old outbox messages",
            deletedCount);
    }

}

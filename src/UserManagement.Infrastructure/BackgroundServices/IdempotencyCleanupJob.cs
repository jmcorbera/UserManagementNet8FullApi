using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Common.Abstractions;
using UserManagement.Application.Common.Options;
using UserManagement.Infrastructure.Options;

namespace UserManagement.Infrastructure.BackgroundServices;

public sealed class IdempotencyCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdempotencyCleanupJob> _logger;
    private readonly CleanupJobOptions _cleanupOptions;
    private readonly IdempotencyOptions _idempotencyOptions;

    public IdempotencyCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<IdempotencyCleanupJob> logger,
        IOptions<CleanupJobOptions> cleanupOptions,
        IOptions<IdempotencyOptions> idempotencyOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cleanupOptions = cleanupOptions.Value;
        _idempotencyOptions = idempotencyOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Idempotency Cleanup Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(_cleanupOptions.CleanupIntervalHours), stoppingToken);

            try
            {
                await CleanupExpiredKeysAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired idempotency keys");
            }
        }

        _logger.LogInformation("Idempotency Cleanup Job stopped");
    }

    private async Task CleanupExpiredKeysAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIdempotencyRepository>();

        var ttl = TimeSpan.FromHours(_idempotencyOptions.TtlHours);

        await PerformCleanupAsync(repository, ttl, cancellationToken);
    }

    private async Task PerformCleanupAsync(IIdempotencyRepository repository, TimeSpan ttl, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting cleanup of idempotency keys older than {TtlHours} hours",
            ttl.TotalHours);

        var deletedCount = await repository.DeleteExpiredAsync(ttl, cancellationToken);

        _logger.LogInformation(
            "Cleanup completed. Deleted {DeletedCount} expired idempotency keys",
            deletedCount);
    }

}

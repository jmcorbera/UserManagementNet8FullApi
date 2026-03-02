namespace UserManagement.Infrastructure.Options;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelaySeconds { get; set; } = 60;
    public int RetryBackoffMultiplier { get; set; } = 2;
}

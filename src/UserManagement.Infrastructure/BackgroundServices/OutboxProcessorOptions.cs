namespace UserManagement.Infrastructure.BackgroundServices;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
}

namespace UserManagement.Infrastructure.Options;

public sealed class CleanupJobOptions
{
    public const string SectionName = "CleanupJob";

    public int OutboxRetentionDays { get; set; } = 30;
    public int CleanupIntervalHours { get; set; } = 24;
}

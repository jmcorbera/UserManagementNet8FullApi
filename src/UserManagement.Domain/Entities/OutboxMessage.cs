namespace UserManagement.Domain.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    private OutboxMessage(
        Guid id,
        string type,
        string content,
        DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredAt = occurredAt;
        ProcessedAt = null;
        Error = null;
        RetryCount = 0;
        NextRetryAt = null;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }

    public static OutboxMessage Create(string type, string content, DateTimeOffset occurredAt)
    {
        return new OutboxMessage(Guid.NewGuid(), type, content, occurredAt);
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
        Error = null;
        NextRetryAt = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }

    public void IncrementRetry(int initialRetryDelaySeconds, int backoffMultiplier)
    {
        RetryCount++;
        var delaySeconds = initialRetryDelaySeconds * Math.Pow(backoffMultiplier, RetryCount - 1);
        NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
    }

    public bool CanRetry(int maxRetries)
    {
        return RetryCount < maxRetries && !ProcessedAt.HasValue;
    }

    public bool IsReadyForRetry()
    {
        return NextRetryAt.HasValue && DateTimeOffset.UtcNow >= NextRetryAt.Value;
    }

    public bool IsProcessed() => ProcessedAt.HasValue;
}

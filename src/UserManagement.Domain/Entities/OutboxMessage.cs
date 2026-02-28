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
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    public static OutboxMessage Create(string type, string content, DateTimeOffset occurredAt)
    {
        return new OutboxMessage(Guid.NewGuid(), type, content, occurredAt);
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }

    public bool IsProcessed() => ProcessedAt.HasValue;
}

namespace UserManagement.Domain.Entities;

public sealed class IdempotencyKey
{
    private IdempotencyKey() { }

    private IdempotencyKey(
        Guid id,
        Guid key,
        string commandName,
        DateTimeOffset createdAt)
    {
        Id = id;
        Key = key;
        CommandName = commandName;
        CreatedAt = createdAt;
        CompletedAt = null;
        Result = null;
        Status = IdempotencyStatus.InProgress;
    }

    public Guid Id { get; private set; }
    public Guid Key { get; private set; }
    public string CommandName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Result { get; private set; }
    public IdempotencyStatus Status { get; private set; }

    public static IdempotencyKey Create(Guid key, string commandName)
    {
        return new IdempotencyKey(Guid.NewGuid(), key, commandName, DateTimeOffset.UtcNow);
    }

    public void SetResult(string result)
    {
        Result = result;
        CompletedAt = DateTimeOffset.UtcNow;
        Status = IdempotencyStatus.Completed;
    }

    public void SetFailed()
{
    Status = IdempotencyStatus.Failed;
    CompletedAt = DateTimeOffset.UtcNow; 
    Result = null;
}

    public bool IsCompleted() => Status == IdempotencyStatus.Completed;

    public bool IsExpired(TimeSpan ttl) => DateTimeOffset.UtcNow - CreatedAt > ttl;
}

namespace UserManagement.Application.Common.Options;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public int TtlHours { get; set; } = 24;
}

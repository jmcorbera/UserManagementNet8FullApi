namespace UserManagement.Application.Common.Abstractions;

public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }
}

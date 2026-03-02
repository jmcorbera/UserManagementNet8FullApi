namespace UserManagement.Application.Common.Abstractions;

public interface IMessagePublisher
{
    Task PublishMessageAsync(string eventType, string eventContent, CancellationToken cancellationToken = default);
}

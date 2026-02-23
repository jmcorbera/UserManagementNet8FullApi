namespace UserManagement.Domain.Events;

/// <summary>
/// Marcador para todos los Domain Events.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

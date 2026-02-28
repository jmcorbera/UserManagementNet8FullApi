using UserManagement.Domain.Events;
using System.Collections.ObjectModel;

namespace UserManagement.Domain.Common;

public abstract class BaseEntity<T>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public T Id { get; protected set; } = default!;

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
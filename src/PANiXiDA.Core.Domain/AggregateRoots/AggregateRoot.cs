using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Entities;

namespace PANiXiDA.Core.Domain.AggregateRoots;

/// <summary>
/// Represents a domain aggregate root with a strongly typed identifier and collected domain events.
/// </summary>
/// <typeparam name="TId">The aggregate root identifier type.</typeparam>
/// <param name="id">The aggregate root identifier.</param>
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id), IAggregateRoot<TId>
    where TId : struct
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>
    /// Adds a domain event to the aggregate root.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Gets a snapshot of the domain events raised by the aggregate root.
    /// </summary>
    /// <returns>The read-only snapshot of domain events.</returns>
    public IReadOnlyCollection<DomainEvent> GetDomainEvents()
    {
        return Array.AsReadOnly(_domainEvents.ToArray());
    }

    /// <summary>
    /// Clears all domain events raised by the aggregate root.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Entities;

namespace PANiXiDA.Core.Domain.AggregateRoots;

/// <summary>
/// Defines an aggregate root with domain event storage.
/// </summary>
public interface IAggregateRoot : IEntity
{
    /// <summary>
    /// Gets a snapshot of the domain events raised by the aggregate root.
    /// </summary>
    /// <returns>The read-only snapshot of domain events.</returns>
    IReadOnlyCollection<DomainEvent> GetDomainEvents();

    /// <summary>
    /// Clears all domain events raised by the aggregate root.
    /// </summary>
    void ClearDomainEvents();
}

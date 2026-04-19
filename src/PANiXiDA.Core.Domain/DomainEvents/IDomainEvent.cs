namespace PANiXiDA.Core.Domain.DomainEvents;

/// <summary>
/// Defines a domain event with an identifier and UTC occurrence timestamp.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique domain event identifier.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the UTC timestamp when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOnUtc { get; }
}

namespace PANiXiDA.Core.Domain.DomainEvents;

/// <summary>
/// Represents a domain event with a generated identifier and UTC occurrence timestamp.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> record.
    /// </summary>
    protected DomainEvent()
    {
        Id = Guid.CreateVersion7();
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique domain event identifier.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the UTC timestamp when the domain event instance was created.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; private init; }
}

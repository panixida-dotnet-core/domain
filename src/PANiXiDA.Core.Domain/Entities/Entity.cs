namespace PANiXiDA.Core.Domain.Entities;

/// <summary>
/// Represents a domain entity with a strongly typed identifier.
/// </summary>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <param name="id">The entity identifier.</param>
public abstract class Entity<TId>(TId id) : IEntity<TId>
    where TId : struct
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; } = id;
}

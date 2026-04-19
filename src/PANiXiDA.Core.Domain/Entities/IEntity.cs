namespace PANiXiDA.Core.Domain.Entities;

/// <summary>
/// Defines an entity with a strongly typed identifier.
/// </summary>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public interface IEntity<out TId>
    where TId : struct
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    TId Id { get; }
}

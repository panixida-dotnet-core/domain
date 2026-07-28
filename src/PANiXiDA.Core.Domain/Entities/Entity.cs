using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.Entities;

/// <summary>
/// Represents a domain entity with a strongly typed identifier.
/// </summary>
/// <typeparam name="TId">
/// The entity identifier value type that implements <see cref="IStronglyTypedId"/>.
/// </typeparam>
/// <param name="id">The entity identifier.</param>
public abstract class Entity<TId>(TId id) : IEntity
    where TId : struct, IStronglyTypedId
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; } = id;
}

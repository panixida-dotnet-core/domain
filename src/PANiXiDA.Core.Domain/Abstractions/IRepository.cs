using PANiXiDA.Core.Domain.AggregateRoots;

using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.Abstractions;

/// <summary>
/// Defines basic persistence operations for an aggregate root.
/// </summary>
/// <typeparam name="TId">
/// The aggregate root identifier value type that implements <see cref="IStronglyTypedId"/>.
/// </typeparam>
/// <typeparam name="TAggregateRoot">The aggregate root type.</typeparam>
public interface IRepository<TId, TAggregateRoot>
    where TId : struct, IStronglyTypedId
    where TAggregateRoot : class, IAggregateRoot
{
    /// <summary>
    /// Gets an aggregate root by its identifier.
    /// </summary>
    /// <param name="id">The aggregate root identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The aggregate root when found; otherwise, <see langword="null"/>.</returns>
    Task<TAggregateRoot?> GetByIdAsync(TId id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an aggregate root asynchronously.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an aggregate root asynchronously.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an aggregate root asynchronously.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken);
}

namespace PANiXiDA.Core.Domain.Identifiers;

/// <summary>
/// Defines a strongly typed identifier backed by a <see cref="Guid"/> value.
/// </summary>
public interface IStronglyTypedId
{
    /// <summary>
    /// Gets the underlying identifier value.
    /// </summary>
    Guid Value { get; }
}

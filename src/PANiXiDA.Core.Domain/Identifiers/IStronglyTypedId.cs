namespace PANiXiDA.Core.Domain.Identifiers;

/// <summary>
/// Defines a strongly typed identifier contract.
/// </summary>
public interface IStronglyTypedId
{
}

/// <summary>
/// Defines a strongly typed identifier backed by a value.
/// </summary>
/// <typeparam name="TValue">The type of the underlying identifier value.</typeparam>
public interface IStronglyTypedId<TValue> : IStronglyTypedId
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying identifier value.
    /// </summary>
    TValue Value { get; }
}

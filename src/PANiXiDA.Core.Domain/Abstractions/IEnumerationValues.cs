namespace PANiXiDA.Core.Domain.Abstractions;

/// <summary>
/// Provides the declared values of an enumeration without runtime reflection.
/// The implementation is generated for partial enumeration types.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
public interface IEnumerationValues<out TEnumeration>
{
    /// <summary>
    /// Gets the immutable list of values stored in the public static fields declared on the concrete type.
    /// </summary>
    /// <returns>The same list on every call, ordered by identifier and validated for duplicate identifiers and names.</returns>
    static abstract IReadOnlyList<TEnumeration> GetDeclaredValues();
}

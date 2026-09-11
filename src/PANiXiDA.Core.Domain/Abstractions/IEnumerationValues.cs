namespace PANiXiDA.Core.Domain.Abstractions;

/// <summary>
/// Provides the declared values of an enumeration without runtime reflection.
/// The implementation is generated for partial enumeration types.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
public interface IEnumerationValues<out TEnumeration>
{
    /// <summary>
    /// Gets the values stored in the public static fields declared on the concrete type.
    /// </summary>
    /// <returns>The declared values before sorting and duplicate validation.</returns>
    static abstract IEnumerable<TEnumeration> GetDeclaredValues();
}

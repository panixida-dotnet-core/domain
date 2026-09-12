using System.Globalization;
using System.Text;

namespace PANiXiDA.Core.Domain.ValueObjects;

/// <summary>
/// Represents a value object whose equality is based on component values.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    /// <param name="a">The first value object to compare.</param>
    /// <param name="b">The second value object to compare.</param>
    /// <returns><see langword="true"/> if the value objects are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    /// <param name="a">The first value object to compare.</param>
    /// <param name="b">The second value object to compare.</param>
    /// <returns><see langword="true"/> if the value objects are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ValueObject? a, ValueObject? b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Determines whether the current value object is equal to another value object.
    /// </summary>
    /// <param name="other">The value object to compare with the current value object.</param>
    /// <returns><see langword="true"/> if the value objects are equal; otherwise, <see langword="false"/>.</returns>
    public virtual bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Gets the values used to compare this value object with another value object.
    /// </summary>
    /// <returns>The ordered sequence of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Formats the equality components using invariant culture and the supplied component names.
    /// </summary>
    /// <param name="typeName">The value object type name supplied by generated code.</param>
    /// <param name="componentNames">The component names, or an empty span to label components by index.</param>
    /// <returns>The type name followed by the names and values of its equality components.</returns>
    protected string FormatEqualityComponents(string typeName, ReadOnlySpan<string> componentNames)
    {
        var builder = new StringBuilder(typeName).Append(" {");
        int index = 0;
        foreach (var component in GetEqualityComponents())
        {
            builder.Append(index == 0 ? " " : ", ");
            if (index < componentNames.Length)
            {
                builder.Append(componentNames[index]);
            }
            else
            {
                builder.Append('[').Append(index.ToString(CultureInfo.InvariantCulture)).Append(']');
            }

            builder.Append(" = ").Append(component is null ? "null" : Convert.ToString(component, CultureInfo.InvariantCulture));
            index++;
        }

        return builder.Append(" }").ToString();
    }
}

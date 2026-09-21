namespace PANiXiDA.Core.Domain.Enumerations;

/// <summary>
/// Represents an extensible enumeration value with a stable identifier and name.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
/// <param name="id">The stable enumeration value identifier.</param>
/// <param name="name">The enumeration value name.</param>
public abstract class Enumeration<TEnumeration>(int id, string name) : IEquatable<TEnumeration>, IComparable<TEnumeration>
    where TEnumeration : Enumeration<TEnumeration>
{
    /// <summary>
    /// Gets the stable enumeration value identifier.
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    /// Gets the enumeration value name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Determines whether two enumeration values are equal.
    /// </summary>
    /// <param name="left">The first enumeration value to compare.</param>
    /// <param name="right">The second enumeration value to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Id == right.Id;
    }

    /// <summary>
    /// Determines whether two enumeration values are not equal.
    /// </summary>
    /// <param name="left">The first enumeration value to compare.</param>
    /// <param name="right">The second enumeration value to compare.</param>
    /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether the left enumeration value is less than the right enumeration value.
    /// </summary>
    /// <param name="left">The first enumeration value to compare.</param>
    /// <param name="right">The second enumeration value to compare.</param>
    /// <returns><see langword="true"/> if the left value is less than the right value; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        return Compare(left, right) < 0;
    }

    /// <summary>
    /// Determines whether the left enumeration value is less than or equal to the right enumeration value.
    /// </summary>
    /// <param name="left">The first enumeration value to compare.</param>
    /// <param name="right">The second enumeration value to compare.</param>
    /// <returns><see langword="true"/> if the left value is less than or equal to the right value; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        return Compare(left, right) <= 0;
    }

    /// <summary>
    /// Determines whether the left enumeration value is greater than the right enumeration value.
    /// </summary>
    /// <param name="left">The first enumeration value to compare.</param>
    /// <param name="right">The second enumeration value to compare.</param>
    /// <returns><see langword="true"/> if the left value is greater than the right value; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        return Compare(left, right) > 0;
    }

    /// <summary>
    /// Determines whether the left enumeration value is greater than or equal to the right enumeration value.
    /// </summary>
    /// <param name="left">The first enumeration value to compare.</param>
    /// <param name="right">The second enumeration value to compare.</param>
    /// <returns><see langword="true"/> if the left value is greater than or equal to the right value; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        return Compare(left, right) >= 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Determines whether the current enumeration value is equal to another enumeration value.
    /// </summary>
    /// <param name="other">The enumeration value to compare with the current value.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public virtual bool Equals(TEnumeration? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TEnumeration other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(typeof(TEnumeration), Id);
    }

    /// <summary>
    /// Compares the current enumeration value with another value by identifier.
    /// </summary>
    /// <param name="other">The enumeration value to compare with the current value.</param>
    /// <returns>A signed integer that indicates the relative order of the values.</returns>
    public int CompareTo(TEnumeration? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Id.CompareTo(other.Id);
    }

    private static int Compare(Enumeration<TEnumeration>? left, Enumeration<TEnumeration>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        return left.Id.CompareTo(right.Id);
    }
}

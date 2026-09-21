using System.Collections.Frozen;

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

    /// <summary>
    /// Gets an enumeration value by its identifier.
    /// </summary>
    /// <param name="id">The enumeration value identifier.</param>
    /// <param name="values">The lazily initialized enumeration snapshot and lookup indexes.</param>
    /// <returns>The enumeration value with the specified identifier.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified identifier is not declared by the concrete enumeration type.
    /// </exception>
    protected static TEnumeration FromId(int id, Lazy<EnumerationValues> values)
    {
        if (TryFromId(id, values, out var item))
        {
            return item!;
        }

        throw new InvalidOperationException(
            $"'{id}' is not a valid id in {typeof(TEnumeration).Name}");
    }

    /// <summary>
    /// Gets an enumeration value by name after trimming surrounding whitespace.
    /// </summary>
    /// <param name="name">The enumeration value name.</param>
    /// <param name="values">The lazily initialized enumeration snapshot and lookup indexes.</param>
    /// <returns>The enumeration value with the specified name.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified name is not declared by the concrete enumeration type.
    /// </exception>
    protected static TEnumeration FromName(string name, Lazy<EnumerationValues> values)
    {
        if (TryFromName(name, values, out var item))
        {
            return item!;
        }

        throw new InvalidOperationException(
            $"'{name}' is not a valid name in {typeof(TEnumeration).Name}");
    }

    /// <summary>
    /// Tries to get an enumeration value by its identifier.
    /// </summary>
    /// <param name="id">The enumeration value identifier.</param>
    /// <param name="values">The lazily initialized enumeration snapshot and lookup indexes.</param>
    /// <param name="item">When this method returns, contains the matching enumeration value, if found.</param>
    /// <returns><see langword="true"/> if a matching value was found; otherwise, <see langword="false"/>.</returns>
    protected static bool TryFromId(
        int id,
        Lazy<EnumerationValues> values,
        out TEnumeration? item)
    {
        return values.Value.ById.TryGetValue(id, out item);
    }

    /// <summary>
    /// Tries to get an enumeration value by name after trimming surrounding whitespace.
    /// </summary>
    /// <param name="name">The enumeration value name.</param>
    /// <param name="values">The lazily initialized enumeration snapshot and lookup indexes.</param>
    /// <param name="item">When this method returns, contains the matching enumeration value, if found.</param>
    /// <returns><see langword="true"/> if a matching value was found; otherwise, <see langword="false"/>.</returns>
    protected static bool TryFromName(
        string name,
        Lazy<EnumerationValues> values,
        out TEnumeration? item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string trimmedName = name.Trim();
        return values.Value.ByName.TryGetValue(trimmedName, out item);
    }

    /// <summary>
    /// Holds an ordered enumeration snapshot and immutable indexes for generated lookup methods.
    /// </summary>
    protected sealed class EnumerationValues
    {
        /// <summary>
        /// Initializes the snapshot and indexes from the declared enumeration values.
        /// </summary>
        /// <param name="items">The generated list of values, which is sorted and retained by the snapshot.</param>
        /// <exception cref="InvalidOperationException">Thrown when an identifier or name is duplicated.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a declared value has a null name.</exception>
        public EnumerationValues(List<TEnumeration> items)
        {
            var byId = new Dictionary<int, TEnumeration>(items.Count);
            var byName = new Dictionary<string, TEnumeration>(items.Count, StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (!byId.TryAdd(item.Id, item))
                {
                    throw new InvalidOperationException(
                        $"Duplicate id '{item.Id}' in {typeof(TEnumeration).Name}");
                }

                if (!byName.TryAdd(item.Name, item))
                {
                    throw new InvalidOperationException(
                        $"Duplicate name '{item.Name}' in {typeof(TEnumeration).Name}");
                }
            }

            items.Sort(static (left, right) => left.Id.CompareTo(right.Id));
            All = items.AsReadOnly();
            ById = byId.ToFrozenDictionary();
            ByName = byName.ToFrozenDictionary(StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets the declared values ordered by identifier.
        /// </summary>
        public IReadOnlyList<TEnumeration> All { get; }

        /// <summary>
        /// Gets the declared values indexed by identifier.
        /// </summary>
        public FrozenDictionary<int, TEnumeration> ById { get; }

        /// <summary>
        /// Gets the declared values indexed by name with ordinal comparison.
        /// </summary>
        public FrozenDictionary<string, TEnumeration> ByName { get; }
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

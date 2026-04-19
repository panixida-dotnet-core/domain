using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;

namespace PANiXiDA.Core.Domain;

/// <summary>
/// Represents an extensible enumeration value with a stable identifier and name.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
/// <param name="id">The stable enumeration value identifier.</param>
/// <param name="name">The enumeration value name.</param>
public abstract class Enumeration<TEnumeration>(int id, string name) : IEquatable<TEnumeration>, IComparable<TEnumeration>
    where TEnumeration : Enumeration<TEnumeration>
{
    private static readonly Lazy<EnumerationCache> Cache = new(CreateCache);

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
    /// Gets all declared enumeration values of the concrete enumeration type.
    /// </summary>
    /// <returns>The declared enumeration values.</returns>
    public static IReadOnlyList<TEnumeration> GetAll()
    {
        return Cache.Value.Items;
    }

    /// <summary>
    /// Gets an enumeration value by its identifier.
    /// </summary>
    /// <param name="id">The enumeration value identifier.</param>
    /// <returns>The enumeration value with the specified identifier.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified identifier is not declared by the concrete enumeration type.
    /// </exception>
    public static TEnumeration FromId(int id)
    {
        if (Cache.Value.ById.TryGetValue(id, out var item))
        {
            return item;
        }

        throw new InvalidOperationException(
            $"'{id}' is not a valid id in {typeof(TEnumeration).Name}");
    }

    /// <summary>
    /// Gets an enumeration value by its exact name.
    /// </summary>
    /// <param name="name">The exact enumeration value name.</param>
    /// <returns>The enumeration value with the specified name.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified name is not declared by the concrete enumeration type.
    /// </exception>
    public static TEnumeration FromName(string name)
    {
        if (Cache.Value.ByName.TryGetValue(name, out var item))
        {
            return item;
        }

        throw new InvalidOperationException(
            $"'{name}' is not a valid name in {typeof(TEnumeration).Name}");
    }

    /// <summary>
    /// Tries to get an enumeration value by its identifier.
    /// </summary>
    /// <param name="id">The enumeration value identifier.</param>
    /// <param name="item">When this method returns, contains the matching enumeration value, if found.</param>
    /// <returns><see langword="true"/> if a matching value was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromId(int id, out TEnumeration? item)
    {
        return Cache.Value.ById.TryGetValue(id, out item);
    }

    /// <summary>
    /// Tries to get an enumeration value by name after trimming surrounding whitespace.
    /// </summary>
    /// <param name="name">The enumeration value name.</param>
    /// <param name="item">When this method returns, contains the matching enumeration value, if found.</param>
    /// <returns><see langword="true"/> if a matching value was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromName(string name, out TEnumeration? item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return Cache.Value.ByName.TryGetValue(name.Trim(), out item);
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

    private static EnumerationCache CreateCache()
    {
        var fields = typeof(TEnumeration)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        var itemsBuilder = ImmutableArray.CreateBuilder<TEnumeration>(fields.Length);
        var byId = new Dictionary<int, TEnumeration>(fields.Length);
        var byName = new Dictionary<string, TEnumeration>(fields.Length, StringComparer.Ordinal);

        foreach (var field in fields)
        {
            if (field.GetValue(null) is not TEnumeration item)
            {
                continue;
            }

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

            itemsBuilder.Add(item);
        }

        return new EnumerationCache(
            itemsBuilder.ToImmutable(),
            byId.ToFrozenDictionary(),
            byName.ToFrozenDictionary(StringComparer.Ordinal));
    }

    private sealed class EnumerationCache(
        ImmutableArray<TEnumeration> items,
        FrozenDictionary<int, TEnumeration> byId,
        FrozenDictionary<string, TEnumeration> byName)
    {
        public ImmutableArray<TEnumeration> Items { get; } = items;
        public FrozenDictionary<int, TEnumeration> ById { get; } = byId;
        public FrozenDictionary<string, TEnumeration> ByName { get; } = byName;
    }
}

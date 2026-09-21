using PANiXiDA.Core.Domain.Enumerations;

namespace PANiXiDA.Core.Domain.UnitTests.Enumerations;

public sealed partial class EnumerationLookupTests
{
    [Fact(DisplayName = "Indexed lookups preserve all declared instances for large and sparse identifiers")]
    public void Lookups_WithManySparseIdentifiers_ReturnDeclaredInstances()
    {
        // Arrange
        var values = IndexedEnumeration.GetAll();

        // Act and assert
        values.Should().HaveCount(16).And.BeInAscendingOrder(value => value.Id);
        values[0].Should().BeSameAs(IndexedEnumeration.Minimum);
        values[^1].Should().BeSameAs(IndexedEnumeration.Maximum);
        foreach (var expected in values)
        {
            IndexedEnumeration.FromId(expected.Id).Should().BeSameAs(expected);
            IndexedEnumeration.TryFromId(expected.Id, out var byId).Should().BeTrue();
            byId.Should().BeSameAs(expected);
            IndexedEnumeration.FromName(expected.Name).Should().BeSameAs(expected);
            IndexedEnumeration.TryFromName($" {expected.Name} ", out var byName).Should().BeTrue();
            byName.Should().BeSameAs(expected);
        }

        IndexedEnumeration.TryFromId(int.MinValue + 1, out var missingId).Should().BeFalse();
        IndexedEnumeration.TryFromName("Missing", out var missingName).Should().BeFalse();
        missingId.Should().BeNull();
        missingName.Should().BeNull();
    }

    [Fact(DisplayName = "Name indexes distinguish names that differ only by case")]
    public void FromName_WithNamesDifferingByCase_ReturnsExactMatch()
    {
        // Act
        var upper = IndexedEnumeration.FromName("Active");
        var lower = IndexedEnumeration.FromName("active");
        bool found = IndexedEnumeration.TryFromName("ACTIVE", out var missing);

        // Assert
        upper.Should().BeSameAs(IndexedEnumeration.Active);
        lower.Should().BeSameAs(IndexedEnumeration.LowercaseActive);
        found.Should().BeFalse();
        missing.Should().BeNull();
    }

    [Fact(DisplayName = "Indexes and ordering use base enumeration properties when concrete properties hide them")]
    public void Lookups_WithHiddenProperties_UseBaseIdentifierAndName()
    {
        // Act
        var values = HiddenPropertiesEnumeration.GetAll();
        var byId = HiddenPropertiesEnumeration.FromId(1);
        var byName = HiddenPropertiesEnumeration.FromName("First");

        // Assert
        values.Should().Equal(HiddenPropertiesEnumeration.First, HiddenPropertiesEnumeration.Second);
        byId.Should().BeSameAs(HiddenPropertiesEnumeration.First);
        byName.Should().BeSameAs(HiddenPropertiesEnumeration.First);
        HiddenPropertiesEnumeration.TryFromId(99, out _).Should().BeFalse();
        HiddenPropertiesEnumeration.TryFromName("Hidden First", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "A null declared name preserves the key argument exception")]
    public void GetAll_WithNullDeclaredName_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => NullNameEnumeration.GetAll();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    private sealed partial class IndexedEnumeration(int id, string name) : Enumeration<IndexedEnumeration>(id, name)
    {
        public static readonly IndexedEnumeration Maximum = new(int.MaxValue, "Maximum");
        public static readonly IndexedEnumeration Active = new(0, "Active");
        public static readonly IndexedEnumeration LowercaseActive = new(-17, "active");
        public static readonly IndexedEnumeration Minimum = new(int.MinValue, "Minimum");
        public static readonly IndexedEnumeration First = new(1, "First");
        public static readonly IndexedEnumeration Second = new(2, "Second");
        public static readonly IndexedEnumeration Third = new(3, "Third");
        public static readonly IndexedEnumeration Fourth = new(4, "Fourth");
        public static readonly IndexedEnumeration Fifth = new(5, "Fifth");
        public static readonly IndexedEnumeration Sixth = new(6, "Sixth");
        public static readonly IndexedEnumeration Seventh = new(7, "Seventh");
        public static readonly IndexedEnumeration Eighth = new(8, "Eighth");
        public static readonly IndexedEnumeration Ninth = new(9, "Ninth");
        public static readonly IndexedEnumeration Tenth = new(10, "Tenth");
        public static readonly IndexedEnumeration Eleventh = new(11, "Eleventh");
        public static readonly IndexedEnumeration Twelfth = new(12, "Twelfth");
    }

    private sealed partial class HiddenPropertiesEnumeration(int id, string name)
        : Enumeration<HiddenPropertiesEnumeration>(id, name)
    {
        public static readonly HiddenPropertiesEnumeration Second = new(2, "Second");
        public static readonly HiddenPropertiesEnumeration First = new(1, "First");

        public new int Id => base.Id + 98;
        public new string Name => $"Hidden {base.Name}";
    }

    private sealed partial class NullNameEnumeration(int id, string name) : Enumeration<NullNameEnumeration>(id, name)
    {
        public static readonly NullNameEnumeration Item = new(1, null!);
    }
}

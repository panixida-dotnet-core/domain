namespace PANiXiDA.Core.Domain.UnitTests;

public sealed partial class EnumerationGenerationTests
{
    [Fact(DisplayName = "Enumeration discovers fields before any value is accessed")]
    public void FromId_BeforeAccessingFields_ReturnsDeclaredInstance()
    {
        var value = InitiallyUnusedEnumeration.FromId(2);

        value.Should().BeSameAs(InitiallyUnusedEnumeration.Second);
        InitiallyUnusedEnumeration.GetAll().Should().Equal(
            InitiallyUnusedEnumeration.First, InitiallyUnusedEnumeration.Second);
    }

    [Fact(DisplayName = "Enumeration retains the same immutable list between lookups")]
    public void GetAll_AfterRepeatedLookups_ReturnsSameList()
    {
        var first = InitiallyUnusedEnumeration.GetAll();

        var second = InitiallyUnusedEnumeration.GetAll();

        second.Should().BeSameAs(first);
        second.Should().NotBeAssignableTo<InitiallyUnusedEnumeration[]>();
    }

    [Fact(DisplayName = "Enumeration includes values stored in object fields and ignores unrelated members")]
    public void GetAll_WithMixedMembers_ReturnsOnlyDeclaredFieldValues()
    {
        var values = MixedEnumeration.GetAll();

        values.Select(value => value.Id).Should().Equal(1, 2);
        values[1].Should().BeSameAs((MixedEnumeration)MixedEnumeration.Boxed);
    }

    [Fact(DisplayName = "Enumeration keeps runtime identifiers and exact name matching")]
    public void Lookups_WithComputedValues_PreserveLookupRules()
    {
        var value = ComputedEnumeration.FromId(2);

        value.Name.Should().Be("Value-2");
        ComputedEnumeration.FromName("Value-2").Should().BeSameAs(value);
        ComputedEnumeration.TryFromName(" Value-2 ", out var trimmed).Should().BeTrue();
        trimmed.Should().BeSameAs(value);
        ComputedEnumeration.TryFromName("value-2", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Enumeration supports independently initialized generic types")]
    public void GetAll_WithGenericEnumerations_KeepsValuesSeparate()
    {
        var integerValues = GenericEnumeration<int>.GetAll();
        var stringValues = GenericEnumeration<string>.GetAll();

        integerValues.Should().ContainSingle().Which.Should().BeSameAs(GenericEnumeration<int>.Item);
        stringValues.Should().ContainSingle().Which.Should().BeSameAs(GenericEnumeration<string>.Item);
    }

    private sealed partial class InitiallyUnusedEnumeration(int id, string name)
        : Enumeration<InitiallyUnusedEnumeration>(id, name)
    {
        public static readonly InitiallyUnusedEnumeration Second = new(2, "Second");
        public static readonly InitiallyUnusedEnumeration First = new(1, "First");
    }

    private sealed partial class MixedEnumeration(int id, string name) : Enumeration<MixedEnumeration>(id, name)
    {
        public static readonly MixedEnumeration Item = new(1, "Item");
        public static readonly object Boxed = new MixedEnumeration(2, "Boxed");
        public static readonly MixedEnumeration? Missing = null;
        public const string Unrelated = "Unrelated";
        public static MixedEnumeration Property => new(3, "Property");
    }

    private sealed partial class ComputedEnumeration(int id, string name) : Enumeration<ComputedEnumeration>(id, name)
    {
        public static readonly ComputedEnumeration Item = Create();

        private static ComputedEnumeration Create()
        {
            int id = int.Parse("2", System.Globalization.CultureInfo.InvariantCulture);
            return new ComputedEnumeration(id, $"Value-{id}");
        }
    }

    private sealed partial class GenericEnumeration<T>(int id, string name) : Enumeration<GenericEnumeration<T>>(id, name)
    {
        public static readonly GenericEnumeration<T> Item = new(1, "Item");
    }
}

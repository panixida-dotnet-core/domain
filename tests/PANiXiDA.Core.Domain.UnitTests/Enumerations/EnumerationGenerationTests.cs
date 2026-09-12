using PANiXiDA.Core.Domain.Enumerations;

namespace PANiXiDA.Core.Domain.UnitTests.Enumerations;

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

    [Fact(DisplayName = "Generated enumeration lists cannot be modified through a collection interface")]
    public void GetAll_WhenMutatedThroughCollectionInterface_RejectsChanges()
    {
        // Arrange
        var values = (IList<InitiallyUnusedEnumeration>)InitiallyUnusedEnumeration.GetAll();

        // Act
        Action act = () => values[0] = InitiallyUnusedEnumeration.Second;

        // Assert
        act.Should().Throw<NotSupportedException>();
        values[0].Should().BeSameAs(InitiallyUnusedEnumeration.First);
    }

    [Fact(DisplayName = "Generated lists include values assigned by an explicit static constructor")]
    public void FromId_WithStaticConstructor_ReturnsInitializedValue()
    {
        // Act
        var value = StaticConstructorEnumeration.FromId(7);

        // Assert
        value.Should().BeSameAs(StaticConstructorEnumeration.Item);
        StaticConstructorEnumeration.GetAll().Should().ContainSingle();
    }

    [Fact(DisplayName = "Concurrent first lookups return the same generated enumeration list")]
    public async Task GetAll_WithConcurrentFirstAccess_ReturnsSameList()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(ConcurrentEnumeration.GetAll, TestContext.Current.CancellationToken))
            .ToArray();

        // Act
        var lists = await Task.WhenAll(tasks);

        // Assert
        foreach (var list in lists)
        {
            list.Should().BeSameAs(lists[0]);
            list.Should().Equal(ConcurrentEnumeration.First, ConcurrentEnumeration.Second);
        }
    }

    [Fact(DisplayName = "Empty generated enumerations return an empty list and no matching values")]
    public void Lookups_WithEmptyEnumeration_ReturnNoValues()
    {
        // Act
        var values = EmptyEnumeration.GetAll();
        bool foundId = EmptyEnumeration.TryFromId(1, out var byId);
        bool foundName = EmptyEnumeration.TryFromName("Missing", out var byName);

        // Assert
        values.Should().BeEmpty();
        foundId.Should().BeFalse();
        foundName.Should().BeFalse();
        byId.Should().BeNull();
        byName.Should().BeNull();
    }

    [Fact(DisplayName = "Lookup validates all generated values before returning an otherwise valid match")]
    public void FromId_WithDuplicateValues_ThrowsBeforeReturningMatch()
    {
        // Act
        Action act = () => DuplicateEnumeration.FromId(1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Duplicate id '2' in DuplicateEnumeration");
    }

    [Fact(DisplayName = "Null names follow the same failure behavior as other invalid names")]
    public void FromName_WithNullName_ThrowsWhenTryFromNameReturnsFalse()
    {
        // Act
        Action act = () => InitiallyUnusedEnumeration.FromName(null!);
        bool found = InitiallyUnusedEnumeration.TryFromName(null!, out var value);

        // Assert
        found.Should().BeFalse();
        value.Should().BeNull();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("'' is not a valid name in InitiallyUnusedEnumeration");
    }

    [Theory(DisplayName = "Invalid name input is rejected before generated list validation")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void FromName_WithInvalidInput_DoesNotValidateDuplicateValues(string? name)
    {
        // Act
        bool found = DuplicateEnumeration.TryFromName(name!, out var value);
        Action act = () => DuplicateEnumeration.FromName(name!);

        // Assert
        found.Should().BeFalse();
        value.Should().BeNull();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"'{name}' is not a valid name in DuplicateEnumeration");
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

    private sealed partial class StaticConstructorEnumeration(int id, string name)
        : Enumeration<StaticConstructorEnumeration>(id, name)
    {
        public static readonly StaticConstructorEnumeration Item;

        static StaticConstructorEnumeration()
        {
            Item = new(7, "Item");
        }
    }

    private sealed partial class ConcurrentEnumeration(int id, string name) : Enumeration<ConcurrentEnumeration>(id, name)
    {
        public static readonly ConcurrentEnumeration Second = new(2, "Second");
        public static readonly ConcurrentEnumeration First = new(1, "First");
    }

    private sealed partial class EmptyEnumeration(int id, string name) : Enumeration<EmptyEnumeration>(id, name);

    private sealed partial class DuplicateEnumeration(int id, string name) : Enumeration<DuplicateEnumeration>(id, name)
    {
        public static readonly DuplicateEnumeration First = new(1, "First");
        public static readonly DuplicateEnumeration Second = new(2, "Second");
        public static readonly DuplicateEnumeration Duplicate = new(2, "Duplicate");
    }
}

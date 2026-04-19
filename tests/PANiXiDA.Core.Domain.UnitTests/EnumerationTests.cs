namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class EnumerationTests
{
    [Fact(DisplayName = "Enumeration exposes identifier and name")]
    public void Properties_ReturnConstructorValues()
    {
        TestEnumeration item = TestEnumeration.First;

        int id = item.Id;
        string name = item.Name;

        id.Should().Be(1);
        name.Should().Be("First");
    }

    [Fact(DisplayName = "GetAll returns declared enumeration values")]
    public void GetAll_ReturnsDeclaredEnumerationValues()
    {
        IReadOnlyList<TestEnumeration> items = TestEnumeration.GetAll();

        items.Should().Equal(TestEnumeration.First, TestEnumeration.Second);
    }

    [Fact(DisplayName = "GetAll returns enumeration values ordered by identifier")]
    public void GetAll_ReturnsEnumerationValuesOrderedByIdentifier()
    {
        IReadOnlyList<UnorderedEnumeration> items = UnorderedEnumeration.GetAll();

        items.Should().Equal(UnorderedEnumeration.First, UnorderedEnumeration.Second);
    }

    [Fact(DisplayName = "GetAll ignores public static fields with another type")]
    public void GetAll_IgnoresFieldsWithAnotherType()
    {
        IReadOnlyList<EnumerationWithIgnoredField> items = EnumerationWithIgnoredField.GetAll();

        items.Should().Equal(EnumerationWithIgnoredField.Item);
    }

    [Fact(DisplayName = "FromId returns matching enumeration value")]
    public void FromId_WhenIdExists_ReturnsMatchingValue()
    {
        TestEnumeration item = TestEnumeration.FromId(2);

        item.Should().BeSameAs(TestEnumeration.Second);
    }

    [Fact(DisplayName = "FromId throws when identifier is unknown")]
    public void FromId_WhenIdIsUnknown_ThrowsInvalidOperationException()
    {
        Action act = () => TestEnumeration.FromId(99);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("'99' is not a valid id in TestEnumeration");
    }

    [Fact(DisplayName = "FromName returns matching enumeration value")]
    public void FromName_WhenNameExists_ReturnsMatchingValue()
    {
        TestEnumeration item = TestEnumeration.FromName("Second");

        item.Should().BeSameAs(TestEnumeration.Second);
    }

    [Fact(DisplayName = "FromName throws when name is unknown")]
    public void FromName_WhenNameIsUnknown_ThrowsInvalidOperationException()
    {
        Action act = () => TestEnumeration.FromName("Unknown");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("'Unknown' is not a valid name in TestEnumeration");
    }

    [Fact(DisplayName = "TryFromId returns matching enumeration value")]
    public void TryFromId_WhenIdExists_ReturnsTrueAndMatchingValue()
    {
        bool result = TestEnumeration.TryFromId(1, out TestEnumeration? item);

        result.Should().BeTrue();
        item.Should().BeSameAs(TestEnumeration.First);
    }

    [Fact(DisplayName = "TryFromId returns false when identifier is unknown")]
    public void TryFromId_WhenIdIsUnknown_ReturnsFalseAndNull()
    {
        bool result = TestEnumeration.TryFromId(99, out TestEnumeration? item);

        result.Should().BeFalse();
        item.Should().BeNull();
    }

    [Fact(DisplayName = "TryFromName trims name and returns matching enumeration value")]
    public void TryFromName_WhenTrimmedNameExists_ReturnsTrueAndMatchingValue()
    {
        bool result = TestEnumeration.TryFromName(" Second ", out TestEnumeration? item);

        result.Should().BeTrue();
        item.Should().BeSameAs(TestEnumeration.Second);
    }

    [Fact(DisplayName = "TryFromName returns false when name is whitespace")]
    public void TryFromName_WhenNameIsWhiteSpace_ReturnsFalseAndNull()
    {
        bool result = TestEnumeration.TryFromName("   ", out TestEnumeration? item);

        result.Should().BeFalse();
        item.Should().BeNull();
    }

    [Fact(DisplayName = "TryFromName returns false when name is unknown")]
    public void TryFromName_WhenNameIsUnknown_ReturnsFalseAndNull()
    {
        bool result = TestEnumeration.TryFromName("Unknown", out TestEnumeration? item);

        result.Should().BeFalse();
        item.Should().BeNull();
    }

    [Fact(DisplayName = "ToString returns enumeration name")]
    public void ToString_ReturnsName()
    {
        string result = TestEnumeration.First.ToString();

        result.Should().Be("First");
    }

    [Fact(DisplayName = "Equals returns true for the same reference")]
    public void Equals_WhenSameReference_ReturnsTrue()
    {
        bool result = TestEnumeration.First.Equals(TestEnumeration.First);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals returns true for another value with same identifier")]
    public void Equals_WhenIdentifiersAreEqual_ReturnsTrue()
    {
        bool result = DuplicateIdEnumeration.First.Equals(DuplicateIdEnumeration.Second);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals returns false for null")]
    public void Equals_WhenOtherIsNull_ReturnsFalse()
    {
        bool result = TestEnumeration.First.Equals(null);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equals returns false for another identifier")]
    public void Equals_WhenIdentifiersAreDifferent_ReturnsFalse()
    {
        bool result = TestEnumeration.First.Equals(TestEnumeration.Second);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Object Equals returns true for matching enumeration value")]
    public void ObjectEquals_WhenObjectIsEnumeration_ReturnsTrue()
    {
        bool result = TestEnumeration.First.Equals((object)TestEnumeration.First);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Object Equals returns false for another object type")]
    public void ObjectEquals_WhenObjectIsAnotherType_ReturnsFalse()
    {
        bool result = TestEnumeration.First.Equals("First");

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "GetHashCode returns same value for same enumeration type and identifier")]
    public void GetHashCode_WhenIdentifierIsSame_ReturnsSameValue()
    {
        int firstHash = DuplicateIdEnumeration.First.GetHashCode();
        int secondHash = DuplicateIdEnumeration.Second.GetHashCode();

        firstHash.Should().Be(secondHash);
    }

    [Fact(DisplayName = "CompareTo returns one when other value is null")]
    public void CompareTo_WhenOtherIsNull_ReturnsOne()
    {
        int result = TestEnumeration.First.CompareTo(null);

        result.Should().Be(1);
    }

    [Fact(DisplayName = "CompareTo compares values by identifier")]
    public void CompareTo_WhenOtherExists_UsesIdentifier()
    {
        int result = TestEnumeration.First.CompareTo(TestEnumeration.Second);

        result.Should().BeNegative();
    }

    [Fact(DisplayName = "Equality operator returns true for same reference")]
    public void EqualityOperator_WhenSameReference_ReturnsTrue()
    {
        TestEnumeration left = TestEnumeration.First;
        TestEnumeration right = left;

        bool result = left == right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equality operator returns true for equal identifiers")]
    public void EqualityOperator_WhenIdentifiersAreEqual_ReturnsTrue()
    {
        bool result = DuplicateIdEnumeration.First == DuplicateIdEnumeration.Second;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equality operator returns false when left value is null")]
    public void EqualityOperator_WhenLeftIsNull_ReturnsFalse()
    {
        TestEnumeration? left = null;

        bool result = left == TestEnumeration.First;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equality operator returns false when right value is null")]
    public void EqualityOperator_WhenRightIsNull_ReturnsFalse()
    {
        TestEnumeration? right = null;

        bool result = TestEnumeration.First == right;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equality operator returns false for different identifiers")]
    public void EqualityOperator_WhenIdentifiersAreDifferent_ReturnsFalse()
    {
        bool result = TestEnumeration.First == TestEnumeration.Second;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Inequality operator returns false for equal values")]
    public void InequalityOperator_WhenValuesAreEqual_ReturnsFalse()
    {
        TestEnumeration left = TestEnumeration.First;
        TestEnumeration right = left;

        bool result = left != right;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Inequality operator returns true for different values")]
    public void InequalityOperator_WhenValuesAreDifferent_ReturnsTrue()
    {
        bool result = TestEnumeration.First != TestEnumeration.Second;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Less than operator handles same reference")]
    public void LessThanOperator_WhenSameReference_ReturnsFalse()
    {
        TestEnumeration left = TestEnumeration.First;
        TestEnumeration right = left;

        bool result = left < right;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Less than operator returns true when left is null")]
    public void LessThanOperator_WhenLeftIsNull_ReturnsTrue()
    {
        TestEnumeration? left = null;

        bool result = left < TestEnumeration.First;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Less than operator compares identifiers")]
    public void LessThanOperator_WhenLeftIdentifierIsLower_ReturnsTrue()
    {
        bool result = TestEnumeration.First < TestEnumeration.Second;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Less than or equal operator handles same reference")]
    public void LessThanOrEqualOperator_WhenSameReference_ReturnsTrue()
    {
        TestEnumeration left = TestEnumeration.First;
        TestEnumeration right = left;

        bool result = left <= right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Less than or equal operator returns true when left is null")]
    public void LessThanOrEqualOperator_WhenLeftIsNull_ReturnsTrue()
    {
        TestEnumeration? left = null;

        bool result = left <= TestEnumeration.First;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Less than or equal operator compares identifiers")]
    public void LessThanOrEqualOperator_WhenLeftIdentifierIsLower_ReturnsTrue()
    {
        bool result = TestEnumeration.First <= TestEnumeration.Second;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Greater than operator returns true when right is null")]
    public void GreaterThanOperator_WhenRightIsNull_ReturnsTrue()
    {
        TestEnumeration? right = null;

        bool result = TestEnumeration.First > right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Greater than operator compares identifiers")]
    public void GreaterThanOperator_WhenLeftIdentifierIsHigher_ReturnsTrue()
    {
        bool result = TestEnumeration.Second > TestEnumeration.First;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Greater than or equal operator returns true when right is null")]
    public void GreaterThanOrEqualOperator_WhenRightIsNull_ReturnsTrue()
    {
        TestEnumeration? right = null;

        bool result = TestEnumeration.First >= right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Greater than or equal operator compares identifiers")]
    public void GreaterThanOrEqualOperator_WhenLeftIdentifierIsHigher_ReturnsTrue()
    {
        bool result = TestEnumeration.Second >= TestEnumeration.First;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "GetAll throws when duplicate identifiers are declared")]
    public void GetAll_WhenDuplicateIdsExist_ThrowsInvalidOperationException()
    {
        Action act = () => DuplicateIdEnumeration.GetAll();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Duplicate id '1' in DuplicateIdEnumeration");
    }

    [Fact(DisplayName = "GetAll throws when duplicate names are declared")]
    public void GetAll_WhenDuplicateNamesExist_ThrowsInvalidOperationException()
    {
        Action act = () => DuplicateNameEnumeration.GetAll();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Duplicate name 'Same' in DuplicateNameEnumeration");
    }

    private sealed class TestEnumeration(int id, string name) : Enumeration<TestEnumeration>(id, name)
    {
        public static readonly TestEnumeration First = new(1, "First");
        public static readonly TestEnumeration Second = new(2, "Second");
    }

    private sealed class EnumerationWithIgnoredField(int id, string name)
        : Enumeration<EnumerationWithIgnoredField>(id, name)
    {
        public static readonly EnumerationWithIgnoredField Item = new(1, "Item");
        public static readonly string Ignored = "Ignored";
    }

    private sealed class UnorderedEnumeration(int id, string name) : Enumeration<UnorderedEnumeration>(id, name)
    {
        public static readonly UnorderedEnumeration Second = new(2, "Second");
        public static readonly UnorderedEnumeration First = new(1, "First");
    }

    private sealed class DuplicateIdEnumeration(int id, string name)
        : Enumeration<DuplicateIdEnumeration>(id, name)
    {
        public static readonly DuplicateIdEnumeration First = new(1, "First");
        public static readonly DuplicateIdEnumeration Second = new(1, "Second");
    }

    private sealed class DuplicateNameEnumeration(int id, string name)
        : Enumeration<DuplicateNameEnumeration>(id, name)
    {
        public static readonly DuplicateNameEnumeration First = new(1, "Same");
        public static readonly DuplicateNameEnumeration Second = new(2, "Same");
    }
}

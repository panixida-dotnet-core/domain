using System.Globalization;

using PANiXiDA.Core.Domain.ValueObjects;

namespace PANiXiDA.Core.Domain.UnitTests.ValueObjects;

public sealed partial class ValueObjectGenerationTests
{
    [Fact(DisplayName = "Generated equality and string representation use the stored email value")]
    public void Email_WithEqualValues_HasEqualComponentsAndReadableText()
    {
        var first = new Email("user@example.test");
        var second = new Email("user@example.test");
        var other = new Email("other@example.test");

        bool equal = first == second;
        string text = first.ToString();

        equal.Should().BeTrue();
        (first == other).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
        text.Should().Be("Email { Value = user@example.test }");
    }

    [Fact(DisplayName = "Generated token equality and text include both value and expiration")]
    public void UserActionToken_WithDifferentComponents_ComparesEveryStoredProperty()
    {
        var expiration = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var token = new UserActionToken("sample-token", expiration);

        bool same = token == new UserActionToken("sample-token", expiration);
        bool differentValue = token == new UserActionToken("other-token", expiration);
        bool differentExpiration = token == new UserActionToken("sample-token", expiration.AddMinutes(1));

        same.Should().BeTrue();
        differentValue.Should().BeFalse();
        differentExpiration.Should().BeFalse();
        token.ToString().Should().Be(
            "UserActionToken { Value = sample-token, ExpiresAtUtc = 01/02/2030 03:04:05 +00:00 }");
    }

    [Fact(DisplayName = "Generated methods ignore computed properties and mutable or static state")]
    public void Equality_WithUnrelatedMembers_UsesOnlyImmutableAutoProperties()
    {
        var first = new MixedValue("same") { Mutable = "first", Initial = 7 };
        var second = new MixedValue("same") { Mutable = "second", Initial = 7 };
        var other = new MixedValue("same") { Initial = 8 };

        bool equal = first == second;
        string text = first.ToString();

        equal.Should().BeTrue();
        (first == other).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
        text.Should().Be("MixedValue { Value = same, Initial = 7 }");
    }

    [Fact(DisplayName = "Manual equality is preserved and automatic text uses its actual components")]
    public void ManualEquality_WithCustomComponents_RemainsInControlOfEqualityAndText()
    {
        var first = new ManualEquality("value", 1);
        var second = new ManualEquality("VALUE", 2);

        bool equal = first == second;
        string text = first.ToString();

        equal.Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
        text.Should().Be("ManualEquality { [0] = VALUE }");
    }

    [Fact(DisplayName = "Manual ToString is preserved while equality is generated")]
    public void ManualToString_WithGeneratedEquality_KeepsExplicitRepresentation()
    {
        var value = new ManualText("first");

        string text = value.ToString();
        bool equal = value == new ManualText("first");

        text.Should().Be("custom text");
        equal.Should().BeTrue();
        (value == new ManualText("second")).Should().BeFalse();
    }

    [Fact(DisplayName = "Manual implementations of both methods are left unchanged")]
    public void ManualMethods_WhenBothAreDeclared_PreserveCustomBehavior()
    {
        var value = new ManualValue("value");

        bool equal = value == new ManualValue("VALUE");

        equal.Should().BeTrue();
        value.ToString().Should().Be("manual value");
    }

    [Fact(DisplayName = "Generated methods include stored properties inherited from an abstract value object")]
    public void InheritedProperties_WithGeneratedMethods_IncludeBaseAndDerivedState()
    {
        var value = new DerivedValue("first", 7);

        bool equal = value == new DerivedValue("first", 7);

        equal.Should().BeTrue();
        (value == new DerivedValue("second", 7)).Should().BeFalse();
        (value == new DerivedValue("first", 8)).Should().BeFalse();
        value.ToString().Should().Be("DerivedValue { Value = first, Number = 7 }");
    }

    [Fact(DisplayName = "Inherited sealed overrides are not replaced by generated methods")]
    public void InheritedManualMethods_WithPartialDerivedType_PreserveBaseImplementations()
    {
        var first = new DerivedManualValue("value");
        var second = new DerivedManualValue("VALUE");

        bool equal = first == second;

        equal.Should().BeTrue();
        first.ToString().Should().Be("inherited text");
    }

    [Fact(DisplayName = "Generated text represents null components and generic value objects")]
    public void GenericValue_WithNullComponent_FormatsNullAndPreservesEquality()
    {
        var first = new GenericValue<string?>(null);
        var second = new GenericValue<string?>(null);

        bool equal = first == second;

        equal.Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
        first.ToString().Should().Be("GenericValue { Value = null }");
        new GenericValue<int>(42).ToString().Should().Be("GenericValue { Value = 42 }");
    }

    [Fact(DisplayName = "Generated text formats numeric components using invariant culture")]
    public void ToString_WithDifferentCurrentCulture_ProducesStableNumericText()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var value = new GenericValue<decimal>(12.5m);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            string text = value.ToString();

            text.Should().Be("GenericValue { Value = 12.5 }");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact(DisplayName = "Generated text supports an explicitly empty equality component sequence")]
    public void ToString_WithManualEmptyComponents_ReturnsEmptyRepresentation()
    {
        var value = new EmptyValue();

        string text = value.ToString();

        text.Should().Be("EmptyValue { }");
    }

    private sealed partial class Email(string value) : ValueObject
    {
        public const int MaxLength = 320;
        public string Value { get; } = value;
    }

    private sealed partial class UserActionToken(string value, DateTimeOffset expiresAtUtc) : ValueObject
    {
        public string Value { get; } = value;
        public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;
    }

    private sealed partial class MixedValue(string value) : ValueObject
    {
        public string Value { get; } = value;
        public int Initial { get; init; }
        public string Mutable { get; set; } = string.Empty;
        public readonly int Field = 42;
        public static string Static => throw new InvalidOperationException("Static getter must not be called.");
        public string Computed => throw new InvalidOperationException($"Computed getter must not be called for '{Value}'.");
        public string this[int index] => throw new InvalidOperationException("Indexer must not be called.");
        public partial string PartialComputed { get; }
        public partial string PartialAccessorExpression { get; }
        public partial string PartialAccessorBlock { get; }
    }

    private sealed partial class MixedValue
    {
        public partial string PartialComputed => throw new InvalidOperationException($"Unexpected getter for '{Value}'.");

        public partial string PartialAccessorExpression
        {
            get => throw new InvalidOperationException($"Unexpected getter for '{Value}'.");
        }

        public partial string PartialAccessorBlock
        {
            get
            {
                throw new InvalidOperationException($"Unexpected getter for '{Value}'.");
            }
        }
    }

    private sealed partial class ManualEquality(string value, int ignored) : ValueObject
    {
        public string Value { get; } = value;
        public int Ignored { get; } = ignored;
        protected override IEnumerable<object?> GetEqualityComponents() => [Value.ToUpperInvariant()];
    }

    private sealed partial class ManualText(string value) : ValueObject
    {
        public string Value { get; } = value;
        public override string ToString() => "custom text";
    }

    private sealed partial class ManualValue(string value) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() => [value.ToUpperInvariant()];
        public override string ToString() => "manual value";
    }

    private abstract class BaseValue(string value) : ValueObject
    {
        public string Value { get; } = value;
    }

    private sealed partial class DerivedValue(string value, int number) : BaseValue(value)
    {
        public int Number { get; } = number;
    }

    private abstract class ManualBaseValue(string value) : ValueObject
    {
        protected sealed override IEnumerable<object?> GetEqualityComponents() => [value.ToUpperInvariant()];
        public sealed override string ToString() => "inherited text";
    }

    private sealed partial class DerivedManualValue(string value) : ManualBaseValue(value);

    private sealed partial class GenericValue<T>(T value) : ValueObject
    {
        public T Value { get; } = value;
    }

    private sealed partial class EmptyValue : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() => [];
    }
}

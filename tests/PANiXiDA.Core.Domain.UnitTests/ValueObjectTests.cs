namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class ValueObjectTests
{
    [Fact(DisplayName = "Equality operator returns true for same reference")]
    public void EqualityOperator_WhenSameReference_ReturnsTrue()
    {
        TestValueObject valueObject = new("value", 1);
        TestValueObject left = valueObject;
        TestValueObject right = valueObject;

        bool result = left == right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equality operator returns true for equal components")]
    public void EqualityOperator_WhenComponentsAreEqual_ReturnsTrue()
    {
        TestValueObject left = new("value", 1);
        TestValueObject right = new("value", 1);

        bool result = left == right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equality operator returns false when left value is null")]
    public void EqualityOperator_WhenLeftIsNull_ReturnsFalse()
    {
        TestValueObject? left = null;
        TestValueObject right = new("value", 1);

        bool result = left == right;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equality operator returns false when right value is null")]
    public void EqualityOperator_WhenRightIsNull_ReturnsFalse()
    {
        TestValueObject left = new("value", 1);
        TestValueObject? right = null;

        bool result = left == right;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Inequality operator returns false for equal values")]
    public void InequalityOperator_WhenValuesAreEqual_ReturnsFalse()
    {
        TestValueObject left = new("value", 1);
        TestValueObject right = new("value", 1);

        bool result = left != right;

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Inequality operator returns true for different values")]
    public void InequalityOperator_WhenValuesAreDifferent_ReturnsTrue()
    {
        TestValueObject left = new("value", 1);
        TestValueObject right = new("another", 2);

        bool result = left != right;

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals returns false for null")]
    public void Equals_WhenOtherIsNull_ReturnsFalse()
    {
        TestValueObject valueObject = new("value", 1);

        bool result = valueObject.Equals(null);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equals returns true for same reference")]
    public void Equals_WhenSameReference_ReturnsTrue()
    {
        TestValueObject valueObject = new("value", 1);

        bool result = valueObject.Equals(valueObject);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals returns false for another value object type")]
    public void Equals_WhenOtherTypeIsDifferent_ReturnsFalse()
    {
        TestValueObject valueObject = new("value", 1);
        OtherValueObject other = new("value", 1);

        bool result = valueObject.Equals(other);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Equals returns true for equal components")]
    public void Equals_WhenComponentsAreEqual_ReturnsTrue()
    {
        TestValueObject left = new("value", 1);
        TestValueObject right = new("value", 1);

        bool result = left.Equals(right);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Equals returns false for different components")]
    public void Equals_WhenComponentsAreDifferent_ReturnsFalse()
    {
        TestValueObject left = new("value", 1);
        TestValueObject right = new("another", 2);

        bool result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Object Equals returns true for equal value object")]
    public void ObjectEquals_WhenObjectIsEqualValueObject_ReturnsTrue()
    {
        TestValueObject left = new("value", 1);
        object right = new TestValueObject("value", 1);

        bool result = left.Equals(right);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Object Equals returns false for another object type")]
    public void ObjectEquals_WhenObjectIsAnotherType_ReturnsFalse()
    {
        TestValueObject valueObject = new("value", 1);

        bool result = valueObject.Equals("value");

        result.Should().BeFalse();
    }

    [Fact(DisplayName = "GetHashCode returns same value for equal components")]
    public void GetHashCode_WhenComponentsAreEqual_ReturnsSameValue()
    {
        TestValueObject left = new("value", 1);
        TestValueObject right = new("value", 1);

        int leftHash = left.GetHashCode();
        int rightHash = right.GetHashCode();

        leftHash.Should().Be(rightHash);
    }

    [Fact(DisplayName = "GetHashCode supports empty components")]
    public void GetHashCode_WhenComponentsAreEmpty_ReturnsHashCode()
    {
        EmptyValueObject valueObject = new();

        int hashCode = valueObject.GetHashCode();

        hashCode.Should().Be(valueObject.GetHashCode());
    }

    private sealed class TestValueObject(string text, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return text;
            yield return number;
        }
    }

    private sealed class OtherValueObject(string text, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return text;
            yield return number;
        }
    }

    private sealed class EmptyValueObject : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            return [];
        }
    }
}

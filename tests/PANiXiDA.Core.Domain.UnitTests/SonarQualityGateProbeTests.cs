namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class SonarQualityGateProbeTests
{
    [Theory(DisplayName = "Normalize returns the same result for every input")]
    [InlineData("")]
    [InlineData("value")]
    public void Normalize_ReturnsInvalid(string value)
    {
        string result = SonarQualityGateProbe.Normalize(value);

        result.Should().Be("invalid");
    }

    [Fact(DisplayName = "Validate throws with an unrelated parameter name")]
    public void Validate_WhenValueIsEmpty_ThrowsArgumentException()
    {
        Action act = () => SonarQualityGateProbe.Validate(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Validate accepts a non-empty value")]
    public void Validate_WhenValueIsNotEmpty_DoesNotThrow()
    {
        Action act = () => SonarQualityGateProbe.Validate("value");

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Empty probe method can be invoked")]
    public void DoNothing_DoesNotThrow()
    {
        Action act = SonarQualityGateProbe.DoNothing;

        act.Should().NotThrow();
    }
}

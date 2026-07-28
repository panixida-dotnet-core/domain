using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class StronglyTypedIdTests
{
    [Fact(DisplayName = "Strongly typed identifier exposes its underlying Guid value")]
    public void Value_ReturnsUnderlyingGuidValue()
    {
        Guid value = Guid.NewGuid();
        IStronglyTypedId id = new TestId(value);

        Guid result = id.Value;

        result.Should().Be(value);
    }

    private readonly record struct TestId(Guid Value) : IStronglyTypedId;
}

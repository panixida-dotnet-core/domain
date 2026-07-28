using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class StronglyTypedIdTests
{
    [Fact(DisplayName = "Strongly typed identifier exposes its underlying value")]
    public void Value_ReturnsUnderlyingValue()
    {
        Guid value = Guid.NewGuid();
        IStronglyTypedId<Guid> id = new TestId(value);

        Guid result = id.Value;

        result.Should().Be(value);
    }

    [Fact(DisplayName = "Generic strongly typed identifier implements the non-generic contract")]
    public void GenericContract_ImplementsNonGenericContract()
    {
        TestId id = new(Guid.NewGuid());

        IStronglyTypedId result = id;

        result.Should().Be(id);
    }

    private readonly record struct TestId(Guid Value) : IStronglyTypedId<Guid>;
}

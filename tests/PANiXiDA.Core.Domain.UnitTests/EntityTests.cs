using PANiXiDA.Core.Domain.Entities;

using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class EntityTests
{
    [Fact(DisplayName = "Entity exposes its identifier")]
    public void Id_ReturnsConstructorValue()
    {
        TestId id = new(Guid.NewGuid());
        TestEntity entity = new(id);

        TestId result = entity.Id;

        result.Should().Be(id);
    }

    [Fact(DisplayName = "Entity implements entity contract")]
    public void Entity_ImplementsEntityContract()
    {
        TestEntity entity = new(new TestId(Guid.NewGuid()));

        IEntity contract = entity;

        contract.Should().BeSameAs(entity);
    }

    [Fact(DisplayName = "Entity contract does not expose identifier")]
    public void EntityContract_DoesNotExposeIdentifier()
    {
        typeof(IEntity).IsGenericType.Should().BeFalse();
        typeof(IEntity).GetProperties().Should().BeEmpty();
    }

    [Fact(DisplayName = "Entity identifier requires the strongly typed identifier contract")]
    public void IdentifierTypeParameter_RequiresStronglyTypedIdentifierContract()
    {
        Type identifierTypeParameter = typeof(Entity<>).GetGenericArguments().Single();

        Type[] constraints = identifierTypeParameter.GetGenericParameterConstraints();

        constraints.Should().Contain(typeof(IStronglyTypedId));
    }

    private readonly record struct TestId(Guid Value) : IStronglyTypedId;

    private sealed class TestEntity(TestId id) : Entity<TestId>(id);
}

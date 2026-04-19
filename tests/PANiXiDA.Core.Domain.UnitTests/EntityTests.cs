using PANiXiDA.Core.Domain.Entities;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class EntityTests
{
    [Fact(DisplayName = "Entity exposes its identifier")]
    public void Id_ReturnsConstructorValue()
    {
        Guid id = Guid.NewGuid();
        TestEntity entity = new(id);

        Guid result = entity.Id;

        result.Should().Be(id);
    }

    [Fact(DisplayName = "Entity implements entity contract")]
    public void Entity_ImplementsEntityContract()
    {
        TestEntity entity = new(Guid.NewGuid());

        IEntity contract = entity;

        contract.Should().BeSameAs(entity);
    }

    [Fact(DisplayName = "Entity contract does not expose identifier")]
    public void EntityContract_DoesNotExposeIdentifier()
    {
        typeof(IEntity).IsGenericType.Should().BeFalse();
        typeof(IEntity).GetProperties().Should().BeEmpty();
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);
}

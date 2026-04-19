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

        IEntity<Guid> contract = entity;

        contract.Id.Should().Be(entity.Id);
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);
}

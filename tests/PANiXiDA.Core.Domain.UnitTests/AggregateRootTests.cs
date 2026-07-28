using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class AggregateRootTests
{
    [Fact(DisplayName = "Aggregate root exposes its identifier")]
    public void Id_ReturnsConstructorValue()
    {
        TestId id = new(42);
        TestAggregateRoot aggregateRoot = new(id);

        TestId result = aggregateRoot.Id;

        result.Should().Be(id);
    }

    [Fact(DisplayName = "Aggregate root implements aggregate root contract")]
    public void AggregateRoot_ImplementsAggregateRootContract()
    {
        TestAggregateRoot aggregateRoot = new(new TestId(42));

        IAggregateRoot contract = aggregateRoot;

        contract.GetDomainEvents().Should().BeEmpty();
    }

    [Fact(DisplayName = "Aggregate root contract does not expose identifier")]
    public void AggregateRootContract_DoesNotExposeIdentifier()
    {
        typeof(IAggregateRoot).IsGenericType.Should().BeFalse();
        typeof(IAggregateRoot).GetProperties().Should().BeEmpty();
    }

    [Fact(DisplayName = "GetDomainEvents returns raised domain events")]
    public void GetDomainEvents_ReturnsRaisedDomainEvents()
    {
        TestAggregateRoot aggregateRoot = new(new TestId(42));
        TestDomainEvent domainEvent = new();

        aggregateRoot.Raise(domainEvent);

        aggregateRoot.GetDomainEvents().Should().Equal(domainEvent);
    }

    [Fact(DisplayName = "GetDomainEvents returns snapshot of raised domain events")]
    public void GetDomainEvents_ReturnsSnapshotOfRaisedDomainEvents()
    {
        TestAggregateRoot aggregateRoot = new(new TestId(42));
        TestDomainEvent domainEvent = new();
        aggregateRoot.Raise(domainEvent);

        IReadOnlyCollection<DomainEvent> domainEvents = aggregateRoot.GetDomainEvents();
        aggregateRoot.ClearDomainEvents();

        domainEvents.Should().Equal(domainEvent);
        aggregateRoot.GetDomainEvents().Should().BeEmpty();
    }

    [Fact(DisplayName = "ClearDomainEvents removes raised domain events")]
    public void ClearDomainEvents_RemovesRaisedDomainEvents()
    {
        TestAggregateRoot aggregateRoot = new(new TestId(42));
        aggregateRoot.Raise(new TestDomainEvent());

        aggregateRoot.ClearDomainEvents();

        aggregateRoot.GetDomainEvents().Should().BeEmpty();
    }

    [Fact(DisplayName = "Aggregate root identifier requires the strongly typed identifier contract")]
    public void IdentifierTypeParameter_RequiresStronglyTypedIdentifierContract()
    {
        Type identifierTypeParameter = typeof(AggregateRoot<>).GetGenericArguments().Single();

        Type[] constraints = identifierTypeParameter.GetGenericParameterConstraints();

        constraints.Should().Contain(typeof(IStronglyTypedId));
    }

    private readonly record struct TestId(int Value) : IStronglyTypedId<int>;

    private sealed class TestAggregateRoot(TestId id) : AggregateRoot<TestId>(id)
    {
        public void Raise(DomainEvent domainEvent)
        {
            AddDomainEvent(domainEvent);
        }
    }

    private sealed record TestDomainEvent : DomainEvent;
}

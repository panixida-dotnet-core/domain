using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class AggregateRootTests
{
    [Fact(DisplayName = "Aggregate root exposes its identifier")]
    public void Id_ReturnsConstructorValue()
    {
        TestAggregateRoot aggregateRoot = new(42);

        int id = aggregateRoot.Id;

        id.Should().Be(42);
    }

    [Fact(DisplayName = "Aggregate root implements aggregate root contract")]
    public void AggregateRoot_ImplementsAggregateRootContract()
    {
        TestAggregateRoot aggregateRoot = new(42);

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
        TestAggregateRoot aggregateRoot = new(42);
        TestDomainEvent domainEvent = new();

        aggregateRoot.Raise(domainEvent);

        aggregateRoot.GetDomainEvents().Should().Equal(domainEvent);
    }

    [Fact(DisplayName = "GetDomainEvents returns snapshot of raised domain events")]
    public void GetDomainEvents_ReturnsSnapshotOfRaisedDomainEvents()
    {
        TestAggregateRoot aggregateRoot = new(42);
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
        TestAggregateRoot aggregateRoot = new(42);
        aggregateRoot.Raise(new TestDomainEvent());

        aggregateRoot.ClearDomainEvents();

        aggregateRoot.GetDomainEvents().Should().BeEmpty();
    }

    private sealed class TestAggregateRoot(int id) : AggregateRoot<int>(id)
    {
        public void Raise(DomainEvent domainEvent)
        {
            AddDomainEvent(domainEvent);
        }
    }

    private sealed record TestDomainEvent : DomainEvent;
}

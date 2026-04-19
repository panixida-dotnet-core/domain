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

    [Fact(DisplayName = "GetDomainEvents returns raised domain events")]
    public void GetDomainEvents_ReturnsRaisedDomainEvents()
    {
        TestAggregateRoot aggregateRoot = new(42);
        TestDomainEvent domainEvent = new();

        aggregateRoot.Raise(domainEvent);

        aggregateRoot.GetDomainEvents().Should().Equal(domainEvent);
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

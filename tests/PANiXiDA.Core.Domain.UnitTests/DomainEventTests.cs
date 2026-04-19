using PANiXiDA.Core.Domain.DomainEvents;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class DomainEventTests
{
    [Fact(DisplayName = "Domain event creates identifier and UTC timestamp")]
    public void Constructor_CreatesIdentifierAndUtcTimestamp()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        TestDomainEvent domainEvent = new();
        DateTimeOffset after = DateTimeOffset.UtcNow;

        domainEvent.Id.Should().NotBe(Guid.Empty);
        domainEvent.OccurredOnUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        domainEvent.OccurredOnUtc.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact(DisplayName = "Domain event implements domain event contract")]
    public void DomainEvent_ImplementsDomainEventContract()
    {
        TestDomainEvent domainEvent = new();

        IDomainEvent contract = domainEvent;

        contract.Id.Should().Be(domainEvent.Id);
        contract.OccurredOnUtc.Should().Be(domainEvent.OccurredOnUtc);
    }

    [Fact(DisplayName = "Domain event supports record equality")]
    public void Equals_WhenSameRecordReference_ReturnsTrue()
    {
        TestDomainEvent domainEvent = new();

        bool result = domainEvent.Equals(domainEvent);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Domain event supports record copy")]
    public void WithExpression_CopiesDomainEventValues()
    {
        TestDomainEvent domainEvent = new();

        TestDomainEvent copy = domainEvent with { };

        copy.Should().NotBeSameAs(domainEvent);
        copy.Id.Should().Be(domainEvent.Id);
        copy.OccurredOnUtc.Should().Be(domainEvent.OccurredOnUtc);
    }

    [Fact(DisplayName = "Domain event supports record text representation")]
    public void ToString_ReturnsRecordTextRepresentation()
    {
        TestDomainEvent domainEvent = new();

        string result = domainEvent.ToString();

        result.Should().StartWith("TestDomainEvent");
    }

    private sealed record TestDomainEvent : DomainEvent;
}

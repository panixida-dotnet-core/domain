# PANiXiDA.Core.Domain

[![CI](https://github.com/panixida-dotnet-core/domain/actions/workflows/ci.yml/badge.svg)](https://github.com/panixida-dotnet-core/domain/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/PANiXiDA.Core.Domain.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Domain)
[![NuGet downloads](https://img.shields.io/nuget/dt/PANiXiDA.Core.Domain.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Domain)
[![Target Framework](https://img.shields.io/badge/target-net10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/panixida-dotnet-core/domain.svg)](LICENSE)

`PANiXiDA.Core.Domain` provides small, reusable domain model building blocks for .NET applications that use Domain-Driven Design patterns.

The package contains base abstractions for entities, aggregate roots, domain events, value objects, and extensible enumerations. It is intentionally lightweight and does not require runtime configuration or infrastructure dependencies.

## Installation

### Package Manager

```bash
dotnet add package PANiXiDA.Core.Domain
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="PANiXiDA.Core.Domain" Version="1.0.1" />
</ItemGroup>
```

## Requirements

- .NET 10
- Nullable reference types enabled in consuming projects is recommended

## Features

- Strongly typed `Entity<TId>` base class and `IEntity<TId>` contract.
- `AggregateRoot<TId>` base class with domain event collection support.
- `DomainEvent` base record with generated version 7 `Guid` identifiers and UTC timestamps.
- `ValueObject` base class with component-based equality.
- `Enumeration<TEnumeration>` base class for smart enum-style domain concepts.
- Deterministic lookup behavior for enumeration values by identifier or name.

## Namespaces

```csharp
using PANiXiDA.Core.Domain;
using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Entities;
```

## Entity

Use `Entity<TId>` for domain objects identified by a stable value. The identifier type must be a value type.

```csharp
using PANiXiDA.Core.Domain.Entities;

public sealed class Customer(Guid id) : Entity<Guid>(id)
{
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }
}
```

## Aggregate Root and Domain Events

Use `AggregateRoot<TId>` when an entity is the consistency boundary for a domain model and needs to collect domain events.

```csharp
using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;

public sealed class Order(Guid id) : AggregateRoot<Guid>(id)
{
    public bool IsStarted { get; private set; }

    public void Start()
    {
        if (IsStarted)
        {
            return;
        }

        IsStarted = true;

        AddDomainEvent(new OrderStarted(Id));
    }
}

public sealed record OrderStarted(Guid OrderId) : DomainEvent;
```

Domain events are stored inside the aggregate root until the application layer reads and clears them.

```csharp
Order order = new(Guid.NewGuid());
order.Start();

IReadOnlyCollection<DomainEvent> domainEvents = order.GetDomainEvents();

order.ClearDomainEvents();
```

`DomainEvent` assigns:

- `Id` with `Guid.CreateVersion7()`;
- `OccurredOnUtc` with `DateTimeOffset.UtcNow`.

The package only stores domain events. It does not dispatch, publish, persist, or serialize them.

## Value Object

Use `ValueObject` for immutable concepts where equality is based on values instead of identity.

```csharp
using PANiXiDA.Core.Domain;

public sealed class Money(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

```csharp
Money first = new(10m, "USD");
Money second = new(10m, "USD");

bool areEqual = first == second;
```

Value object equality uses:

- the same runtime type;
- the ordered sequence returned by `GetEqualityComponents()`.

## Enumeration

Use `Enumeration<TEnumeration>` for stable, named domain values that need behavior and lookup methods.

```csharp
using PANiXiDA.Core.Domain;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Draft = new(1, "Draft");
    public static readonly OrderStatus Submitted = new(2, "Submitted");
    public static readonly OrderStatus Cancelled = new(3, "Cancelled");

    private OrderStatus(int id, string name)
        : base(id, name)
    {
    }
}
```

```csharp
OrderStatus submitted = OrderStatus.FromId(2);
OrderStatus cancelled = OrderStatus.FromName("Cancelled");

bool found = OrderStatus.TryFromName(" Submitted ", out OrderStatus? status);
IReadOnlyList<OrderStatus> allStatuses = OrderStatus.GetAll();
```

Enumeration behavior:

- `GetAll()` returns public static values declared on the concrete type ordered by `Id`.
- `FromId(int)` and `FromName(string)` return a value or throw `InvalidOperationException`.
- `TryFromId(int, out TEnumeration?)` returns `false` when no value exists.
- `TryFromName(string, out TEnumeration?)` trims surrounding whitespace and returns `false` for empty or whitespace names.
- Names are compared with `StringComparer.Ordinal`.
- Duplicate identifiers or names throw `InvalidOperationException` during cache creation.
- Equality and ordering are based on identifiers within the concrete enumeration type.

## Configuration

The package does not require runtime configuration, environment variables, external services, or dependency injection registration.

## Development

### Restore

```bash
dotnet restore
```

### Format

```bash
dotnet format
```

### Build

```bash
dotnet build --configuration Release
```

### Test

```bash
dotnet test --configuration Release
```

### Test with Coverage

```bash
dotnet test --configuration Release --coverage --coverage-output-format xml --coverage-output coverage.xml --results-directory TestResults
```

### Pack

```bash
dotnet pack --configuration Release
```

## Repository Layout

```text
.
|-- src/
|   `-- PANiXiDA.Core.Domain/
|-- tests/
|   `-- PANiXiDA.Core.Domain.UnitTests/
|-- Directory.Build.props
|-- Directory.Build.targets
|-- Directory.Packages.props
|-- global.json
|-- version.json
|-- LICENSE
`-- README.md
```

## Package Metadata

- Package ID: `PANiXiDA.Core.Domain`
- Target framework: `net10.0`
- Repository: `https://github.com/panixida-dotnet-core/domain`
- License: Apache-2.0
- Versioning: Nerdbank.GitVersioning

## License

This project is licensed under the Apache-2.0 license.

See the [LICENSE](LICENSE) file for details.

## Maintainers

Maintained by PANiXiDA.

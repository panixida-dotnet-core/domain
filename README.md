# PANiXiDA.Core.Domain

[![CI](https://github.com/panixida-dotnet-core/domain/actions/workflows/ci.yml/badge.svg)](https://github.com/panixida-dotnet-core/domain/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/PANiXiDA.Core.Domain.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Domain)
[![NuGet downloads](https://img.shields.io/nuget/dt/PANiXiDA.Core.Domain.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Domain)
[![Target Framework](https://img.shields.io/badge/target-net10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/panixida-dotnet-core/domain.svg)](LICENSE)

`PANiXiDA.Core.Domain` provides small, reusable domain model building blocks for .NET applications that use Domain-Driven Design patterns.

The package contains base abstractions for strongly typed identifiers, entities, aggregate roots, aggregate repositories, domain events, value objects, and extensible enumerations. It is intentionally lightweight and does not require runtime configuration or infrastructure dependencies.

## Installation

### Package Manager

```bash
dotnet add package PANiXiDA.Core.Domain
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="PANiXiDA.Core.Domain" Version="3.0.0" />
</ItemGroup>
```

## Requirements

- .NET 10
- Nullable reference types enabled in consuming projects is recommended

## Features

- Strongly typed `Entity<TId>` base class and non-generic `IEntity` contract.
- `AggregateRoot<TId>` base class and non-generic `IAggregateRoot` contract with domain event collection support.
- `IStronglyTypedId` contract for domain identifiers backed by `Guid` values.
- `IRepository<TId, TAggregateRoot>` contract for loading and persisting aggregate roots.
- `DomainEvent` base record with generated version 7 `Guid` identifiers and UTC timestamps.
- `ValueObject` base class with component-based equality.
- `Enumeration<TEnumeration>` base class for smart enum-style domain concepts.
- Deterministic lookup behavior for enumeration values by identifier or name.
- Source-generated enumeration lookups, value object methods, and identifier string representations.

## Namespaces

```csharp
using PANiXiDA.Core.Domain.Abstractions;
using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Entities;
using PANiXiDA.Core.Domain.Enumerations;
using PANiXiDA.Core.Domain.Identifiers;
using PANiXiDA.Core.Domain.ValueObjects;
```

## Source Generation

Source generators are included in the package. Reference it directly in each
project declaring generated types. Declare the type and any containing types as
`partial`; file-local types are not supported. Constructors, factories, and
validation are written manually.

With project references, also reference the generator project as an analyzer with
`OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`.

## Strongly Typed Identifier

Use `IStronglyTypedId` to distinguish domain identifiers from raw `Guid` values while keeping the underlying value type consistent.

```csharp
using PANiXiDA.Core.Domain.Identifiers;

public readonly partial record struct CustomerId(Guid Value) : IStronglyTypedId
{
    public static CustomerId New()
    {
        return new CustomerId(Guid.CreateVersion7());
    }
}
```

For partial structs and record structs, the generator adds `ToString()` returning
`Value.ToString()`. An explicitly written `ToString()` takes precedence.

## Entity

Use `Entity<TId>` for domain objects identified by a stable value.
The identifier type must be a value type that implements `IStronglyTypedId`.
The `IEntity` contract is intentionally non-generic and does not expose identifiers; `Id` remains available on `Entity<TId>` implementations.

```csharp
using PANiXiDA.Core.Domain.Entities;

public sealed class Customer(CustomerId id) : Entity<CustomerId>(id)
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
Its identifier type must be a value type that implements `IStronglyTypedId`.
The `IAggregateRoot` contract is intentionally non-generic and does not expose identifiers; `Id` remains available on `AggregateRoot<TId>` implementations.

```csharp
using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Identifiers;

public readonly record struct OrderId(Guid Value) : IStronglyTypedId;

public sealed class Order(OrderId id) : AggregateRoot<OrderId>(id)
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

public sealed record OrderStarted(OrderId OrderId) : DomainEvent;
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

## Repository Abstraction Ownership

Repository contracts are split by architectural responsibility:

| Contract | Package | Namespace | Responsibility |
| --- | --- | --- | --- |
| `IRepository<TId, TAggregateRoot>` | `PANiXiDA.Core.Domain` | `PANiXiDA.Core.Domain.Abstractions` | Loading and persisting aggregate roots through the domain boundary. |
| `IReadRepository<TId>` | [`PANiXiDA.Core.Application`](https://github.com/panixida-dotnet-core/application#repository-abstraction-ownership) | `PANiXiDA.Core.Application.Persistence` | Read-side existence checks used by application queries and validation. |

Use `IRepository<TId, TAggregateRoot>` as the base contract for a repository that works with an aggregate root.
The identifier type must be a value type that implements `IStronglyTypedId`.

```csharp
using PANiXiDA.Core.Domain.Abstractions;

public interface IOrderRepository : IRepository<OrderId, Order>
{
}
```

The contract provides `GetByIdAsync`, `AddAsync`, `UpdateAsync`, and `DeleteAsync`.
Read-only application concerns should depend on `IReadRepository<TId>` from `PANiXiDA.Core.Application`.

## Value Object

Use `ValueObject` for immutable concepts where equality is based on values instead of identity.

```csharp
using PANiXiDA.Core.Domain.ValueObjects;

public sealed partial class Money(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;
}
```

```csharp
Money first = new(10m, "USD");
Money second = new(10m, "USD");

bool areEqual = first == second;
string text = first.ToString();
```

Here, `text` is `Money { Amount = 10, Currency = USD }`.

Value object equality uses:

- the same runtime type;
- the ordered sequence returned by `GetEqualityComponents()`.

For partial value objects, the generator supplies `GetEqualityComponents()` and
`ToString()`. Components are public instance auto-properties with a public getter
and either no setter or an `init` accessor, including inherited properties.
Fields, indexers, mutable properties, and computed getters are excluded.

Override either method to customize it independently; manual overrides, including
inherited ones, take precedence. For example, generate equality with custom text:

```csharp
public sealed partial class Email(string value) : ValueObject
{
    public string Value { get; } = value;

    public override string ToString() => Value;
}
```

Generated `ToString()` prints component names and values using invariant formatting.
With manual `GetEqualityComponents()`, labels are `[0]`, `[1]`, etc.
If there are no eligible auto-properties, implement `GetEqualityComponents()` explicitly.

## Enumeration

Use `Enumeration<TEnumeration>` for stable, named domain values that need behavior and lookup methods.

```csharp
using PANiXiDA.Core.Domain.Enumerations;

public sealed partial class OrderStatus : Enumeration<OrderStatus>
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

The generator adds `GetAll`, `FromId`, `FromName`, `TryFromId`, and `TryFromName`
directly to the partial class. Declare values as public static readonly fields.
Do not call lookup methods from static field initializers or declare methods with
the same signatures as the generated methods.

Enumeration behavior:

- `GetAll()` returns public static values declared on the concrete type ordered by `Id`.
- `FromId(int)` and `FromName(string)` return a value or throw `InvalidOperationException`.
- `TryFromId(int, out TEnumeration?)` returns `false` when no value exists.
- `FromName(string)` and `TryFromName(string, out TEnumeration?)` share the same lookup and trim surrounding whitespace.
- `TryFromName` returns `false` for null, empty, whitespace, or unknown names; `FromName` throws `InvalidOperationException` in each of these cases.
- Names are compared with `StringComparer.Ordinal`.
- Duplicate identifiers or names throw `InvalidOperationException` on first use.
- Equality and ordering are based on identifiers within the concrete enumeration type.

## Configuration

The package does not require runtime configuration, environment variables, external services, or dependency injection registration.

## Development

### Restore

```bash
dotnet restore
```

### Format

Build the source generator before formatting to resolve generated members.

```bash
dotnet build src/PANiXiDA.Core.Domain.Generators/PANiXiDA.Core.Domain.Generators.csproj --no-restore
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

### Continuous integration

Every pull request and push to `main` runs formatting, tests, and mandatory
SonarQube analysis. Publishing from `main` starts only after the SonarQube
Quality Gate succeeds.

## Repository Layout

```text
.
|-- src/
|   |-- PANiXiDA.Core.Domain.Generators/
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

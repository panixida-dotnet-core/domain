# PANiXiDA.Core.Domain

[![CI](https://github.com/panixida-dotnet-core/domain/actions/workflows/ci.yml/badge.svg)](https://github.com/panixida-dotnet-core/domain/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/PANiXiDA.Core.Domain.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Domain)
[![NuGet downloads](https://img.shields.io/nuget/dt/PANiXiDA.Core.Domain.svg)](https://www.nuget.org/packages/PANiXiDA.Core.Domain)
[![Target Framework](https://img.shields.io/badge/target-net10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/panixida-dotnet-core/domain.svg)](LICENSE)

Domain building blocks for .NET 10: strongly typed identifiers, entities, aggregate roots, repositories, domain events, value objects, and enumerations.

## Installation

```bash
dotnet add package PANiXiDA.Core.Domain
```

No runtime configuration or dependency injection registration is required.

The package includes source generators for identifiers, value objects, and enumerations. Reference it directly in each project declaring these types. To use generation, declare the type and any containing types as `partial`; file-local types are not supported. Constructors, factories, and validation are written manually.

## Strongly Typed Identifier

Implement `IStronglyTypedId` with a `Guid` value:

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

```csharp
CustomerId customerId = CustomerId.New();
string customerIdText = customerId.ToString();
```

For partial structs and record structs, the generator adds `ToString()` returning `Value.ToString()`. An explicitly written `ToString()` takes precedence.

## Entity

Inherit `Entity<TId>` for an object with a stable identity. Its identifier must be a struct implementing `IStronglyTypedId`.

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

Use `AggregateRoot<TId>` to collect events raised by an aggregate:

```csharp
using PANiXiDA.Core.Domain.AggregateRoots;
using PANiXiDA.Core.Domain.DomainEvents;
using PANiXiDA.Core.Domain.Identifiers;

public readonly partial record struct OrderId(Guid Value) : IStronglyTypedId;

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

```csharp
Order order = new(new OrderId(Guid.CreateVersion7()));
order.Start();

IReadOnlyCollection<DomainEvent> domainEvents = order.GetDomainEvents();
order.ClearDomainEvents();
```

`DomainEvent` assigns a version 7 `Guid` to `Id` and the current UTC timestamp to `OccurredOnUtc`. Read and process the collected events before clearing them. Event dispatch and persistence belong to the application layer.

## Repository

Define an aggregate repository through `IRepository<TId, TAggregateRoot>`:

```csharp
using PANiXiDA.Core.Domain.Abstractions;

public interface IOrderRepository : IRepository<OrderId, Order>
{
}
```

The contract provides `GetByIdAsync`, `AddAsync`, `UpdateAsync`, and `DeleteAsync`, each accepting a cancellation token. Implement persistence in the infrastructure layer.

## Value Object

Inherit `ValueObject` and declare the class as `partial` to generate equality components and `ToString()`:

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
string moneyText = first.ToString();
```

Here, `areEqual` is `true` and `moneyText` is `Money { Amount = 10, Currency = USD }`.

- Equality requires the same concrete type and equal component values.
- Components are public instance auto-properties with a public getter and either no setter or an `init` accessor, including inherited properties. Mutable and computed properties, fields, and indexers are excluded.
- `ToString()` prints component names and values using invariant formatting.
- Override `GetEqualityComponents()` or `ToString()` to customize either behavior independently. Manual overrides, including inherited ones, take precedence. With manual equality components, generated text uses `[0]`, `[1]`, etc. as labels.

For example, generate equality while supplying a custom string representation:

```csharp
public sealed partial class Email(string value) : ValueObject
{
    public string Value { get; } = value;

    public override string ToString() => Value;
}
```

If there are no eligible auto-properties, implement `GetEqualityComponents()` explicitly.

## Enumeration

Declare a partial class inheriting `Enumeration<TEnumeration>` and its values as public static readonly fields:

```csharp
using PANiXiDA.Core.Domain.Enumerations;

public sealed partial class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Draft = new(1, nameof(Draft));
    public static readonly OrderStatus Submitted = new(2, nameof(Submitted));
    public static readonly OrderStatus Cancelled = new(3, nameof(Cancelled));

    private OrderStatus(int id, string name)
        : base(id, name)
    {
    }
}
```

Call the generated methods on the concrete type:

```csharp
OrderStatus submitted = OrderStatus.FromId(2);
OrderStatus cancelled = OrderStatus.FromName("Cancelled");

bool foundById = OrderStatus.TryFromId(1, out OrderStatus? draft);
bool foundByName = OrderStatus.TryFromName(" Submitted ", out OrderStatus? status);
IReadOnlyList<OrderStatus> allStatuses = OrderStatus.GetAll();
```

- `GetAll()` returns an immutable list of the concrete type's declared values, ordered by `Id`.
- `FromId` and `FromName` throw `InvalidOperationException` if no value is found; the Try methods return `false` and `null`.
- Name lookup trims surrounding whitespace and compares names case-sensitively using `StringComparer.Ordinal`. Null, empty, and whitespace-only names are treated as not found.
- Identifiers and names must be unique; duplicates cause `InvalidOperationException`.
- Equality and ordering use `Id` within the concrete enumeration type.

Do not call lookup methods from static field initializers or declare methods with the same signatures as the generated methods.

## Development

Build the generator before formatting so the formatter can resolve generated members:

```bash
dotnet restore
dotnet build src/PANiXiDA.Core.Domain.Generators/PANiXiDA.Core.Domain.Generators.csproj --no-restore
dotnet format
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack --configuration Release
```

When consuming the library through project references, also reference the generator project with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`.

CI checks formatting, tests, 100% line and branch coverage, and the SonarQube Quality Gate.

## License

[Apache-2.0](LICENSE).

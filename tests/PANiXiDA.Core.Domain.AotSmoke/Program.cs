using System.Globalization;
using System.Runtime.CompilerServices;

using PANiXiDA.Core.Domain;

if (args.Contains("--require-aot", StringComparer.Ordinal))
{
    Check(!RuntimeFeature.IsDynamicCodeSupported, "The smoke test must run as a Native AOT executable.");
}

// Start with a lookup to verify initialization without first touching any declared value.
var values = Status.GetAll();
Check(values.Select(value => value.Id).SequenceEqual([1, 2]), "Values must survive trimming and retain identifier order.");
Check(ReferenceEquals(values, Status.GetAll()), "The immutable snapshot must be reused.");
Check(ReferenceEquals(Status.FromId(2), Status.Second), "Identifier lookup must return the declared instance.");
Check(ReferenceEquals(Status.FromName("Value-2"), Status.Second), "Computed names must remain available.");
Check(Status.TryFromName(" Value-2 ", out var second) && ReferenceEquals(second, Status.Second),
    "TryFromName must trim whitespace.");
Check(!Status.TryFromName("value-2", out _), "Name lookup must remain case-sensitive.");
Check(!Status.TryFromId(99, out _), "Unknown identifiers must not match.");
Check(GenericStatus<int>.GetAll().Count == 1 && GenericStatus<string>.GetAll().Count == 1,
    "Closed generic enumerations must initialize independently.");
Check(EmptyStatus.GetAll().Count == 0, "An enumeration without declared values must remain empty.");

try
{
    DuplicateStatus.GetAll();
    throw new InvalidOperationException("Duplicate identifiers were accepted.");
}
catch (InvalidOperationException exception) when (exception.Message.Contains("Duplicate id", StringComparison.Ordinal))
{
    // Duplicate validation must also run in the published executable.
}

Console.WriteLine("Enumeration package smoke test passed.");

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed partial class Status(int id, string name) : Enumeration<Status>(id, name)
{
    public static readonly Status Second = Create();
    public static readonly object First = new Status(1, "First");
    public static readonly Status? Missing = null;
    public const string Unrelated = "Unrelated";
    public static Status Property => new(3, "Property");

    private static Status Create()
    {
        int id = int.Parse("2", CultureInfo.InvariantCulture);
        return new Status(id, $"Value-{id}");
    }
}

internal sealed partial class GenericStatus<T>(int id, string name) : Enumeration<GenericStatus<T>>(id, name)
{
    public static readonly GenericStatus<T> Item = new(1, "Item");
}

internal sealed partial class EmptyStatus(int id, string name) : Enumeration<EmptyStatus>(id, name);

internal sealed partial class DuplicateStatus(int id, string name) : Enumeration<DuplicateStatus>(id, name)
{
    public static readonly DuplicateStatus First = new(1, "First");
    public static readonly DuplicateStatus Second = new(1, "Second");
}

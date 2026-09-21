using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using PANiXiDA.Core.Domain.Enumerations;
using PANiXiDA.Core.Domain.Generators.Enumerations;

namespace PANiXiDA.Core.Domain.UnitTests.Enumerations;

public sealed class EnumerationGeneratorTests
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
    private static readonly MetadataReference[] References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(Enumeration<>).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
    ];

    [Fact(DisplayName = "Generator emits direct field access and lookup methods on the concrete type")]
    public void Generate_WithPartialEnumeration_EmitsDirectFieldAccess()
    {
        var compilation = CreateCompilation("""
            namespace Example;
            public sealed partial class Status(int id, string name)
                : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name)
            {
                public static readonly Status First = new(1, "First");
                public static readonly object Boxed = new Status(2, "Second");
                private static readonly Status Hidden = new(3, "Hidden");
                public static Status Property => Hidden;
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().ContainAll("global::Example.Status.@First", "global::Example.Status.@Boxed");
        source.Should().NotContain(".@Hidden").And.NotContain(".@Property");
        source.Should().NotContain("System.Reflection").And.NotContain("GetFields");
        var status = output.GetTypeByMetadataName("Example.Status")!;
        foreach (string methodName in new[] { "GetAll", "FromId", "FromName", "TryFromId", "TryFromName" })
        {
            var method = status.GetMembers(methodName).OfType<IMethodSymbol>().Should().ContainSingle().Subject;
            method.IsStatic.Should().BeTrue();
            method.DeclaredAccessibility.Should().Be(Accessibility.Public);
        }
    }

    [Fact(DisplayName = "Generator recognizes Enumeration independently of its generic parameter name")]
    public void Generate_WithRenamedBaseTypeParameter_EmitsCompilableLookups()
    {
        var compilation = CreateCompilation("""
            using System;
            using System.Collections.Generic;

            namespace PANiXiDA.Core.Domain.Enumerations
            {
                public abstract class Enumeration<T>(int id, string name) where T : Enumeration<T>
                {
                    public int Id { get; } = id;
                    public string Name { get; } = name;

                    protected static T FromId(
                        int id,
                        Lazy<EnumerationValues> values) => throw new NotImplementedException();

                    protected static T FromName(
                        string name,
                        Lazy<EnumerationValues> values) => throw new NotImplementedException();

                    protected static bool TryFromId(
                        int id,
                        Lazy<EnumerationValues> values,
                        out T? item) => throw new NotImplementedException();

                    protected static bool TryFromName(
                        string name,
                        Lazy<EnumerationValues> values,
                        out T? item) => throw new NotImplementedException();

                    protected sealed class EnumerationValues(List<T> items)
                    {
                        public IReadOnlyList<T> All { get; } = items.AsReadOnly();
                    }
                }
            }

            namespace Example
            {
                public sealed partial class Status(int id, string name)
                    : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name)
                {
                    public static readonly Status Active = new(1, "Active");
                }
            }
            """).WithReferences(References.Where(reference => reference.Display != typeof(Enumeration<>).Assembly.Location));

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().ContainSingle();
        AssertCompiles(output);
        output.GetTypeByMetadataName("Example.Status")!.GetMembers("FromId").Should().ContainSingle();
    }

    [Fact(DisplayName = "Generator ignores a same-named enumeration from an extern-alias-only assembly")]
    public void Generate_WithForeignEnumeration_DoesNotEmitSource()
    {
        var foreignCompilation = CreateCompilation("""
            namespace PANiXiDA.Core.Domain.Enumerations;
            public abstract class Enumeration<TEnumeration>;
            """).WithAssemblyName("ForeignDomain");
        using var stream = new MemoryStream();
        foreignCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.Should().BeTrue();
        var compilation = CreateCompilation("""
            extern alias foreign;
            public sealed partial class Status
                : foreign::PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>;
            """).AddReferences(MetadataReference.CreateFromImage(stream.ToArray()).WithAliases(["foreign"]));

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
        AssertCompiles(output);
    }

    [Fact(DisplayName = "Generator handles compilations without Enumeration")]
    public void Generate_WithoutEnumerationReference_DoesNotEmitSource()
    {
        var compilation = CreateCompilation("public class Unrelated : System.Exception;")
            .WithReferences(References.Where(reference => reference.Display != typeof(Enumeration<>).Assembly.Location));

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
        AssertCompiles(output);
    }

    [Fact(DisplayName = "Generator combines partial declarations without duplicate output")]
    public void Generate_WithMultiplePartialDeclarations_EmitsOneCompleteProvider()
    {
        var compilation = CreateCompilation("""
            public partial class Status(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name)
            {
                public static readonly Status First = new(1, "First");
            }
            """, """
            public partial class Status : System.IDisposable
            {
                public static readonly Status Second = new(2, "Second");
                public void Dispose() { }
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().ContainAll(".@First", ".@Second");
    }

    [Fact(DisplayName = "Generator avoids collisions with existing list field names")]
    public void Generate_WithExistingValuesField_EmitsCompilableProvider()
    {
        var compilation = CreateCompilation("""
            public abstract class Base<T>(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<T>(id, name)
                where T : Base<T>
            {
                protected const int __enumerationValues = 1;
            }
            public sealed partial class Status<__enumerationValues__>(int id, string name)
                : Base<Status<__enumerationValues__>>(id, name)
            {
                public static readonly Status<__enumerationValues__> __enumerationValues_ = new(1, "First");
            }
            """);

        var (result, output) = Generate(compilation);

        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().BeEmpty();
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().ContainSingle();
    }

    [Theory(DisplayName = "Generator supports nested generic types and escaped identifiers")]
    [InlineData("partial class")]
    [InlineData("static partial class")]
    [InlineData("partial struct")]
    [InlineData("readonly partial struct")]
    [InlineData("ref partial struct")]
    [InlineData("partial record")]
    [InlineData("partial record struct")]
    [InlineData("partial interface")]
    public void Generate_WithNestedEnumeration_PreservesContainingType(string kind)
    {
        var compilation = CreateCompilation($$"""
            namespace @event;
            public {{kind}} Container<T> where T : class
            {
                private sealed partial class @class(int id, string name)
                    : PANiXiDA.Core.Domain.Enumerations.Enumeration<@class>(id, name)
                {
                    public static readonly @class @default = new(1, "First");
                }
            }
            """);

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        AssertCompiles(output);
        result.Results.Single().GeneratedSources.Should().ContainSingle();
    }

    [Theory(DisplayName = "Generator reports actionable diagnostics for unsupported declarations")]
    [InlineData("public class Status", "PANENUM001", "Status")]
    [InlineData("file partial class Status", "PANENUM002", "Status")]
    [InlineData("public class Container { public partial class Status", "PANENUM001", "Container")]
    public void Generate_WithUnsupportedDeclaration_ReportsDiagnostic(string declaration, string id, string typeName)
    {
        string source = declaration + "(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name) { }"
            + (typeName == "Container" ? " }" : string.Empty);
        var compilation = CreateCompilation(source);

        var (result, _) = Generate(compilation);

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(id);
        diagnostic.GetMessage().Should().Contain(typeName);
        diagnostic.Location.GetLineSpan().Path.Should().Be("Source0.cs");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Generator ignores unrelated classes and generic enumeration base types")]
    public void Generate_WithUnrelatedOrGenericBaseTypes_DoesNotEmitSource()
    {
        var compilation = CreateCompilation("""
            public class Unrelated : System.Exception { }
            public abstract class Generic<T>(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<T>(id, name)
                where T : Generic<T>;
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Generator supports inheritance and excludes inherited fields")]
    public void Generate_WithIndirectInheritance_ReadsOnlyConcreteTypeFields()
    {
        var compilation = CreateCompilation("""
            public abstract class Intermediate<T>(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<T>(id, name)
                where T : Intermediate<T>
            {
                public static readonly object Inherited = new object();
            }
            public sealed partial class Status(int id, string name) : Intermediate<Status>(id, name)
            {
                public static readonly Status Item = new(1, "Item");
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain(".@Item").And.NotContain(".@Inherited");
    }

    [Fact(DisplayName = "Generated lookup methods support delegates and generic consumers without a provider constraint")]
    public void Generate_WithGenericConsumer_CompilesUsingOnlyEnumerationConstraint()
    {
        var compilation = CreateCompilation("""
            using System;
            using System.Collections.Generic;
            using PANiXiDA.Core.Domain.Enumerations;

            public sealed partial class Status(int id, string name) : Enumeration<Status>(id, name)
            {
                public static readonly Status Active = new(1, "Active");
            }

            public static class Consumer
            {
                public static T Find<T>(Func<int, T> fromId, int id) where T : Enumeration<T> => fromId(id);
                public static IReadOnlyList<T> Read<T>(Func<IReadOnlyList<T>> getAll)
                    where T : Enumeration<T> => getAll();

                public static Status FindActive() => Find(Status.FromId, 1);
                public static IReadOnlyList<Status> ReadStatuses() => Read(Status.GetAll);
                public static Status FindByName() => Status.FromName(" Active ");
                public static bool TryById(out Status? value) => Status.TryFromId(1, out value);
                public static bool TryByName(out Status? value) => Status.TryFromName("Active", out value);
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact(DisplayName = "Generator preserves unchanged output when unrelated source changes")]
    public void Generate_AfterUnrelatedEdit_ReusesGeneratedOutput()
    {
        var compilation = CreateCompilation("""
            public partial class Status(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name)
            {
                public static readonly Status Item = new(1, "Item");
            }
            """);
        GeneratorDriver driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }", ParseOptions,
            cancellationToken: TestContext.Current.CancellationToken));

        driver = driver.RunGenerators(updated, TestContext.Current.CancellationToken);

        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps.Values
            .SelectMany(steps => steps).SelectMany(step => step.Outputs);
        outputs.Should().NotBeEmpty().And.OnlyContain(output => output.Reason == IncrementalStepRunReason.Cached);
    }

    [Fact(DisplayName = "Generator updates field access after a value is added")]
    public void Generate_AfterAddingValue_UpdatesProvider()
    {
        var compilation = CreateCompilation("""
            public partial class Status(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name)
            {
                public static readonly Status First = new(1, "First");
            }
            """);
        GeneratorDriver driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("""
            public partial class Status
            {
                public static readonly Status Second = new(2, "Second");
            }
            """, ParseOptions, cancellationToken: TestContext.Current.CancellationToken));

        driver = driver.RunGeneratorsAndUpdateCompilation(updated, out var output, out _,
            TestContext.Current.CancellationToken);

        AssertCompiles(output);
        string source = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().ContainAll(".@First", ".@Second");
    }

    [Theory(DisplayName = "Generator refreshes values after editing a secondary partial without a base list")]
    [InlineData(false)]
    [InlineData(true)]
    public void Generate_AfterEditingSecondaryPartial_UpdatesProvider(bool removeValue)
    {
        const string withoutValue = "public partial class Status { }";
        const string withValue = """
            public partial class Status
            {
                public static readonly Status Second = new(2, "Second");
            }
            """;
        var compilation = CreateCompilation("""
            public partial class Status(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name)
            {
                public static readonly Status First = new(1, "First");
            }
            """,
            removeValue ? withValue : withoutValue);
        GeneratorDriver driver = CreateDriver().RunGeneratorsAndUpdateCompilation(
            compilation,
            out var initialOutput,
            out _,
            TestContext.Current.CancellationToken);
        AssertCompiles(initialOutput);
        string initialSource = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();
        var secondaryTree = compilation.SyntaxTrees.Last();
        var editedTree = secondaryTree.WithChangedText(SourceText.From(removeValue ? withoutValue : withValue));
        var updated = compilation.ReplaceSyntaxTree(secondaryTree, editedTree);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            updated,
            out var output,
            out _,
            TestContext.Current.CancellationToken);

        updated.SyntaxTrees.First().Should().BeSameAs(compilation.SyntaxTrees.First());
        AssertCompiles(output);
        var result = driver.GetRunResult();
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().NotBe(initialSource).And.Contain(".@First");
        if (removeValue)
        {
            source.Should().NotContain(".@Second");
        }
        else
        {
            source.Should().Contain(".@Second");
        }

        var (freshResult, _) = Generate(updated);
        source.Should().Be(freshResult.Results.Single().GeneratedSources.Single().SourceText.ToString());
    }

    [Fact(DisplayName = "Generator removes diagnostics after a declaration is made partial")]
    public void Generate_AfterAddingPartial_ReplacesDiagnosticWithProvider()
    {
        const string original = "public class Status(int id, string name) : PANiXiDA.Core.Domain.Enumerations.Enumeration<Status>(id, name) { }";
        var compilation = CreateCompilation(original);
        GeneratorDriver driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.Should().ContainSingle().Which.Id.Should().Be("PANENUM001");
        var corrected = CSharpSyntaxTree.ParseText(original.Replace("public class", "public partial class", StringComparison.Ordinal),
            ParseOptions, "Source0.cs", cancellationToken: TestContext.Current.CancellationToken);
        var updated = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), corrected);

        driver = driver.RunGeneratorsAndUpdateCompilation(updated, out var output, out _,
            TestContext.Current.CancellationToken);

        AssertCompiles(output);
        driver.GetRunResult().Diagnostics.Should().BeEmpty();
        driver.GetRunResult().Results.Single().GeneratedSources.Should().ContainSingle();
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        return CSharpCompilation.Create("GeneratorTest",
            sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, $"Source{index}.cs")),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static CSharpGeneratorDriver CreateDriver()
    {
        return CSharpGeneratorDriver.Create([new EnumerationGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    private static (GeneratorDriverRunResult Result, Compilation Output) Generate(CSharpCompilation compilation)
    {
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    private static void AssertCompiles(Compilation compilation)
    {
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }
}

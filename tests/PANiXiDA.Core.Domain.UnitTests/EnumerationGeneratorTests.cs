using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using PANiXiDA.Core.Domain.Generators;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class EnumerationGeneratorTests
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Append(typeof(Enumeration<>).Assembly.Location)
        .Distinct(StringComparer.Ordinal)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    [Fact(DisplayName = "Generator emits direct field access and a compilable static contract")]
    public void Generate_WithPartialEnumeration_EmitsDirectFieldAccess()
    {
        var compilation = CreateCompilation("""
            namespace Example;
            public sealed partial class Status(int id, string name)
                : PANiXiDA.Core.Domain.Enumeration<Status>(id, name)
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
        source.Should().Contain("global::Example.Status.@First", "global::Example.Status.@Boxed");
        source.Should().NotContain(".@Hidden").And.NotContain(".@Property");
        source.Should().NotContain("System.Reflection").And.NotContain("GetFields");
    }

    [Fact(DisplayName = "Generator combines partial declarations without duplicate output")]
    public void Generate_WithMultiplePartialDeclarations_EmitsOneCompleteProvider()
    {
        var compilation = CreateCompilation("""
            public partial class Status(int id, string name) : PANiXiDA.Core.Domain.Enumeration<Status>(id, name)
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
        source.Should().Contain(".@First", ".@Second");
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
                    : PANiXiDA.Core.Domain.Enumeration<@class>(id, name)
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
        string source = declaration + "(int id, string name) : PANiXiDA.Core.Domain.Enumeration<Status>(id, name) { }"
            + (typeName == "Container" ? " }" : string.Empty);
        var compilation = CreateCompilation(source);

        var (result, _) = Generate(compilation);

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(id);
        diagnostic.GetMessage().Should().Contain(typeName);
        diagnostic.Location.GetLineSpan().Path.Should().Be("Source0.cs");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Generator ignores unrelated classes and explicitly implemented providers")]
    public void Generate_WithUnrelatedOrManualTypes_DoesNotEmitProviders()
    {
        var compilation = CreateCompilation("""
            public class Unrelated : System.Exception { }
            public sealed class Manual(int id, string name) : PANiXiDA.Core.Domain.Enumeration<Manual>(id, name),
                PANiXiDA.Core.Domain.Abstractions.IEnumerationValues<Manual>
            {
                public static System.Collections.Generic.IEnumerable<Manual> GetDeclaredValues() => [];
            }
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
            public abstract class Intermediate<T>(int id, string name) : PANiXiDA.Core.Domain.Enumeration<T>(id, name)
                where T : Intermediate<T>, PANiXiDA.Core.Domain.Abstractions.IEnumerationValues<T>
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

    [Fact(DisplayName = "Generator preserves unchanged output when unrelated source changes")]
    public void Generate_AfterUnrelatedEdit_ReusesGeneratedOutput()
    {
        var compilation = CreateCompilation("""
            public partial class Status(int id, string name) : PANiXiDA.Core.Domain.Enumeration<Status>(id, name)
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

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        return CSharpCompilation.Create("GeneratorTest",
            sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, $"Source{index}.cs")),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static GeneratorDriver CreateDriver()
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

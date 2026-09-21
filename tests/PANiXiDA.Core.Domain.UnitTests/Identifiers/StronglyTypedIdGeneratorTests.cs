using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using PANiXiDA.Core.Domain.Generators.Identifiers;
using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests.Identifiers;

public sealed class StronglyTypedIdGeneratorTests
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
    private static readonly MetadataReference[] References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(IStronglyTypedId).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
    ];

    [Theory(DisplayName = "Generator emits a Guid string override for partial identifier value types")]
    [InlineData("readonly partial record struct")]
    [InlineData("partial record struct")]
    [InlineData("readonly partial struct")]
    [InlineData("partial struct")]
    [InlineData("ref partial struct")]
    public void Generate_WithPartialIdentifier_EmitsValueToString(string kind)
    {
        var compilation = CreateCompilation($$"""
            public {{kind}} UserId(System.Guid value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId
            {
                public System.Guid Value { get; } = value;
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("return this.Value.ToString();");
        var method = output.GetTypeByMetadataName("UserId")!.GetMembers("ToString")
            .OfType<IMethodSymbol>().Should().ContainSingle().Subject;
        method.IsOverride.Should().BeTrue();
        method.IsImplicitlyDeclared.Should().BeFalse();
        method.DeclaredAccessibility.Should().Be(Accessibility.Public);
    }

    [Theory(DisplayName = "Identifier generation preserves existing types and explicit string overrides")]
    [InlineData("public readonly record struct Id(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;")]
    [InlineData("public partial record Id(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;")]
    [InlineData("public partial struct Unrelated : System.IDisposable { public void Dispose() { } }")]
    [InlineData("public partial interface IDerived : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;")]
    [InlineData("public readonly partial record struct Id(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId { public override string ToString() => Value.ToString(\"N\"); }")]
    public void Generate_WithUnsupportedOrManuallyFormattedType_DoesNotEmitSource(string source)
    {
        var compilation = CreateCompilation(source);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Theory(DisplayName = "Identifier generation reports types that cannot be extended from another file")]
    [InlineData("public class Container", "public readonly partial record struct Id", "PANID001", "Container")]
    [InlineData("file partial class Container", "public readonly partial record struct Id", "PANID002", "Container")]
    [InlineData("", "file readonly partial record struct Id", "PANID002", "Id")]
    public void Generate_WithInvalidDeclaration_ReportsDiagnostic(
        string container,
        string declaration,
        string diagnosticId,
        string typeName)
    {
        string identifier = declaration + "(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;";
        var compilation = CreateCompilation(string.IsNullOrEmpty(container)
            ? identifier
            : container + " { " + identifier + " }");

        var (result, _) = Generate(compilation);

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(diagnosticId);
        diagnostic.GetMessage().Should().Contain("'" + typeName + "'");
        diagnostic.Location.GetLineSpan().Path.Should().Be("Source0.cs");
        diagnostic.Location.SourceSpan.Length.Should().Be(typeName.Length);
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Identifier generation combines partial declarations and ignores string overloads")]
    public void Generate_WithMultiplePartsAndOverloads_EmitsOneOverride()
    {
        var compilation = CreateCompilation("""
            public partial record struct Id(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId
            {
                public string ToString(string format) => Value.ToString(format);
                public string ToString<T>() => typeof(T).Name;
            }
            """, """
            public partial record struct Id : System.IDisposable
            {
                public void Dispose() { }
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().ContainSingle();
    }

    [Theory(DisplayName = "Generated identifier text supports nested generic containers and escaped names")]
    [InlineData("partial class")]
    [InlineData("partial record")]
    [InlineData("partial struct")]
    [InlineData("partial interface")]
    public void Generate_WithNestedExplicitIdentifier_EmitsCompilableOverride(string kind)
    {
        var compilation = CreateCompilation($$"""
            namespace @event;
            public {{kind}} Container<TIdentifier>
            {
                public readonly partial record struct @class<TIdentifier_>(System.Guid Original)
                    : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId
                {
                    System.Guid PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId.Value => Original;
                }
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().ContainSingle();
    }

    [Fact(DisplayName = "Identifier generation ignores an identically named interface from an extern-alias-only assembly")]
    public void Generate_WithForeignIdentifierContract_DoesNotEmitSource()
    {
        var foreignCompilation = CreateCompilation("""
            namespace PANiXiDA.Core.Domain.Identifiers;
            public interface IStronglyTypedId { System.Guid Value { get; } }
            """).WithAssemblyName("ForeignIdentifiers");
        using var stream = new MemoryStream();
        foreignCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.Should().BeTrue();
        var compilation = CreateCompilation("""
            extern alias foreign;
            public readonly partial record struct Id(System.Guid Value)
                : foreign::PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;
            """).AddReferences(MetadataReference.CreateFromImage(stream.ToArray()).WithAliases(["foreign"]));

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Identifier generation handles compilations without the domain contract")]
    public void Generate_WithoutDomainReference_DoesNotEmitSource()
    {
        var compilation = CreateCompilation("public partial struct Unrelated : System.IDisposable { public void Dispose() { } }")
            .WithReferences(References.Where(reference => reference.Display != typeof(IStronglyTypedId).Assembly.Location));

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Adding a manual identifier override removes generated text without restarting the driver")]
    public void Generate_AfterManualOverrideIsAdded_RemovesGeneratedMethod()
    {
        var compilation = CreateCompilation("""
            public partial record struct Id(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;
            """, "public partial record struct Id { }");
        GeneratorDriver driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        driver.GetRunResult().Results.Single().GeneratedSources.Should().ContainSingle();
        var secondaryTree = compilation.SyntaxTrees.Last();
        var editedTree = secondaryTree.WithChangedText(SourceText.From("""
            public partial record struct Id
            {
                public override string ToString() => Value.ToString("N");
            }
            """));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation.ReplaceSyntaxTree(secondaryTree, editedTree),
            out var output,
            out _,
            TestContext.Current.CancellationToken);

        AssertCompiles(output);
        driver.GetRunResult().Diagnostics.Should().BeEmpty();
        driver.GetRunResult().Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Identifier generation reuses output after unrelated source edits")]
    public void Generate_AfterUnrelatedEdit_ReusesGeneratedOutput()
    {
        var compilation = CreateCompilation("""
            public readonly partial record struct Id(System.Guid Value) : PANiXiDA.Core.Domain.Identifiers.IStronglyTypedId;
            """);
        GeneratorDriver driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            "class Unrelated { }",
            ParseOptions,
            cancellationToken: TestContext.Current.CancellationToken));

        driver = driver.RunGenerators(updated, TestContext.Current.CancellationToken);

        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps.Values
            .SelectMany(steps => steps).SelectMany(step => step.Outputs);
        outputs.Should().NotBeEmpty().And.OnlyContain(output => output.Reason == IncrementalStepRunReason.Cached);
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        return CSharpCompilation.Create(
            "StronglyTypedIdGeneratorTest",
            sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, $"Source{index}.cs")),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static CSharpGeneratorDriver CreateDriver()
    {
        return CSharpGeneratorDriver.Create(
            [new StronglyTypedIdGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    private static (GeneratorDriverRunResult Result, Compilation Output) Generate(CSharpCompilation compilation)
    {
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(
            compilation,
            out var output,
            out _,
            TestContext.Current.CancellationToken);
        return (driver.GetRunResult(), output);
    }

    private static void AssertCompiles(Compilation compilation)
    {
        compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().BeEmpty();
    }
}

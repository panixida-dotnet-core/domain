using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using PANiXiDA.Core.Domain.Generators.ValueObjects;
using PANiXiDA.Core.Domain.ValueObjects;

namespace PANiXiDA.Core.Domain.UnitTests.ValueObjects;

public sealed class ValueObjectGeneratorTests
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
    private static readonly MetadataReference[] References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Append(typeof(ValueObject).Assembly.Location)
        .Distinct(StringComparer.Ordinal)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    [Theory(DisplayName = "Generator preserves each manually declared method independently")]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Generate_WithManualMethods_EmitsOnlyMissingOverrides(bool manualEquality, bool manualToString)
    {
        // Arrange
        string equality = manualEquality
            ? "protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => [Value];"
            : string.Empty;
        string toString = manualToString ? "public override string ToString() => Value;" : string.Empty;
        var compilation = CreateCompilation($$"""
            public sealed partial class Email(string value) : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public string Value { get; } = value;
                {{equality}}
                {{toString}}
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        var sources = result.Results.Single().GeneratedSources;
        if (manualEquality && manualToString)
        {
            sources.Should().BeEmpty();
            return;
        }

        string source = sources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Contains("GetEqualityComponents()", StringComparison.Ordinal).Should().Be(!manualEquality);
        source.Contains("public override string ToString()", StringComparison.Ordinal).Should().Be(!manualToString);
        source.Should().NotContain("System.Reflection").And.NotContain("GetProperties");
    }

    [Theory(DisplayName = "Generator supports nested generic value objects and escaped names")]
    [InlineData("partial class")]
    [InlineData("static partial class")]
    [InlineData("partial struct")]
    [InlineData("readonly partial struct")]
    [InlineData("ref partial struct")]
    [InlineData("partial record")]
    [InlineData("partial record struct")]
    [InlineData("partial interface")]
    public void Generate_WithNestedTypes_EmitsCompilableOverrides(string kind)
    {
        // Arrange
        var compilation = CreateCompilation($$"""
            namespace @event;
            public {{kind}} Container<T> where T : class
            {
                private sealed partial class @class(T value) : PANiXiDA.Core.Domain.ValueObjects.ValueObject
                {
                    public T @default { get; } = value;
                }
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("yield return this.@default;");
    }

    [Theory(DisplayName = "Generator reports actionable errors instead of inferring empty equality")]
    [InlineData("public class Value", "public int Number { get; } = 1;", "PANVO001", "Value")]
    [InlineData("file partial class Value", "public int Number { get; } = 1;", "PANVO002", "Value")]
    [InlineData("public class Container { public partial class Value", "public int Number { get; } = 1;", "PANVO001", "Container")]
    [InlineData("file partial class Container { public partial class Value", "public int Number { get; } = 1;", "PANVO002", "Container")]
    [InlineData("public partial class Value", "public int Number => 1;", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public int Number { get; set; }", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public static int Number { get; } = 1;", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public int this[int index] => index;", "PANVO003", "Value")]
    [InlineData("public partial class Value", "private int Number { get; } = 1;", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public int Number { private get; init; }", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public int Number { set { } }", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public int Number { get { return 1; } }", "PANVO003", "Value")]
    [InlineData("public partial class Value", "public int Number { get => 1; }", "PANVO003", "Value")]
    [InlineData("public partial class Value", "", "PANVO003", "Value")]
    public void Generate_WithUnsupportedDeclaration_ReportsDiagnostic(string declaration, string members, string id, string typeName)
    {
        // Arrange
        string source = declaration + " : PANiXiDA.Core.Domain.ValueObjects.ValueObject { " + members + " }"
            + (typeName == "Container" ? " }" : string.Empty);

        // Act
        var (result, _) = Generate(CreateCompilation(source));

        // Assert
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(id);
        diagnostic.GetMessage().Should().Contain(typeName);
        diagnostic.Location.GetLineSpan().Path.Should().Be("Source0.cs");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Existing non-partial value objects and abstract bases need no migration")]
    public void Generate_WithManualNonPartialAndUnrelatedTypes_DoesNotEmitSource()
    {
        // Arrange
        var compilation = CreateCompilation("""
            public class Unrelated : System.Exception { }
            public abstract class AbstractValue : PANiXiDA.Core.Domain.ValueObjects.ValueObject { }
            public sealed class ManualValue : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => [];
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Generator combines partial declarations and ignores method overloads")]
    public void Generate_WithMultiplePartsAndOverloads_EmitsOneCompleteType()
    {
        // Arrange
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int First { get; } = 1;
                public string ToString(string prefix) => prefix;
                public string ToString<T>() => typeof(T).Name;
                public object GetEqualityComponents(int count) => count;
            }
            """, """
            public sealed partial class Value : System.IDisposable
            {
                public string Second { get; } = "second";
                public void Dispose() { }
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("yield return this.@First;", "yield return this.@Second;", "public override string ToString()");
    }

    [Fact(DisplayName = "Generator recognizes inherited auto-properties in referenced assemblies")]
    public void Generate_WithReferencedAbstractBase_IncludesStoredPropertiesOnly()
    {
        // Arrange
        var baseCompilation = CreateCompilation("""
            public abstract class StoredValue : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public string BaseValue { get; } = "base";
                public string Computed
                {
                    [System.Diagnostics.DebuggerStepThrough]
                    get => "computed";
                }
            }
            """).WithAssemblyName("ReferencedValueObjects");
        using var stream = new MemoryStream();
        var emit = baseCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        emit.Success.Should().BeTrue();
        var compilation = CreateCompilation("""
            public sealed partial class DerivedValue : StoredValue
            {
                public int Number { get; } = 7;
            }
            """).AddReferences(MetadataReference.CreateFromImage(stream.ToArray()));

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().Contain("yield return this.@BaseValue;", "yield return this.@Number;");
        source.Should().NotContain(".@Computed");
    }

    [Fact(DisplayName = "Generated overrides inherited from a referenced assembly can be extended with derived state")]
    public void Generate_WithReferencedGeneratedBase_IncludesBaseAndDerivedComponents()
    {
        // Arrange
        var baseCompilation = CreateCompilation("""
            public partial class StoredValue : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public string BaseValue { get; } = "base";
            }
            """).WithAssemblyName("ReferencedGeneratedValueObjects");
        var (_, generatedBase) = Generate(baseCompilation);
        using var stream = new MemoryStream();
        generatedBase.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.Should().BeTrue();
        var compilation = CreateCompilation("""
            public sealed partial class DerivedValue : StoredValue
            {
                public int Number { get; } = 7;
            }
            """).AddReferences(MetadataReference.CreateFromImage(stream.ToArray()));

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().Contain("yield return this.@BaseValue;", "yield return this.@Number;", "public override string ToString()");
    }

    [Theory(DisplayName = "Inherited manual overrides are preserved regardless of unrelated attributes")]
    [InlineData("")]
    [InlineData("[System.Diagnostics.DebuggerStepThrough]")]
    [InlineData("[System.CodeDom.Compiler.GeneratedCode(\"OtherGenerator\", \"1.0\")]")]
    [InlineData("[System.CodeDom.Compiler.GeneratedCode(null!, \"1.0\")]")]
    public void Generate_WithInheritedManualMethods_PreservesBaseImplementations(string attribute)
    {
        // Arrange
        var compilation = CreateCompilation($$"""
            public class ManualBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                {{attribute}}
                protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => ["manual"];
                {{attribute}}
                public override string ToString() => "manual";
            }
            public sealed partial class DerivedValue : ManualBase
            {
                public int Number { get; } = 7;
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Inherited abstract overrides are implemented by the generator")]
    public void Generate_WithInheritedAbstractMethods_ImplementsBothOverrides()
    {
        // Arrange
        var compilation = CreateCompilation("""
            public abstract class AbstractBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                protected abstract override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents();
                public abstract override string ToString();
            }
            public sealed partial class DerivedValue : AbstractBase
            {
                public int Number { get; init; }
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("yield return this.@Number;", "public override string ToString()");
    }

    [Fact(DisplayName = "Generator ignores unimplemented abstract properties while a type is being edited")]
    public void Generate_WithUnimplementedAbstractProperty_UsesOnlyStoredProperties()
    {
        // Arrange
        var compilation = CreateCompilation("""
            public abstract class AbstractBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public abstract string Pending { get; }
            }
            public sealed partial class DerivedValue : AbstractBase
            {
                public int Number { get; } = 7;
            }
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("yield return this.@Number;").And.NotContain("this.@Pending");
        var diagnostic = output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("CS0534");
        diagnostic.GetMessage().Should().Contain("Pending");
    }

    [Theory(DisplayName = "Invalid attributes do not cause the generator to replace inherited manual methods")]
    [InlineData("[Missing]", "CS0246")]
    [InlineData("[System.CodeDom.Compiler.GeneratedCode]", "CS7036")]
    public void Generate_WithInvalidInheritedMethodAttribute_PreservesManualMethods(
        string attribute,
        string diagnosticId)
    {
        // Arrange
        var compilation = CreateCompilation($$"""
            public class ManualBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                {{attribute}}
                protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => ["manual"];
                public override string ToString() => "manual";
            }
            public sealed partial class DerivedValue : ManualBase;
            """);

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().NotBeEmpty().And.OnlyContain(item => item.Id == diagnosticId);
    }

    [Fact(DisplayName = "Missing metadata attribute dependencies do not change inherited methods or property selection")]
    public void Generate_WithUnavailableAttributeAssembly_PreservesManualMethodsAndStoredProperties()
    {
        // Arrange
        var attributeCompilation = CreateCompilation("""
            public sealed class MarkerAttribute : System.Attribute;
            """).WithAssemblyName("OptionalAttributes");
        using var attributeStream = new MemoryStream();
        attributeCompilation.Emit(attributeStream, cancellationToken: TestContext.Current.CancellationToken)
            .Success.Should().BeTrue();
        var baseCompilation = CreateCompilation("""
            public class ManualBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                [Marker]
                protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => ["manual"];
                public override string ToString() => "manual";
            }
            public abstract class StoredBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public string Stored { get; } = "stored";
                public string Computed
                {
                    [Marker]
                    get => "computed";
                }
            }
            """).WithAssemblyName("AttributedValueObjects")
            .AddReferences(MetadataReference.CreateFromImage(attributeStream.ToArray()));
        using var baseStream = new MemoryStream();
        baseCompilation.Emit(baseStream, cancellationToken: TestContext.Current.CancellationToken)
            .Success.Should().BeTrue();
        var compilation = CreateCompilation("""
            public sealed partial class ManualValue : ManualBase;
            public sealed partial class StoredValue : StoredBase
            {
                public int Number { get; } = 7;
            }
            """).AddReferences(MetadataReference.CreateFromImage(baseStream.ToArray()));

        // Act
        var (result, output) = Generate(compilation);

        // Assert
        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("yield return this.@Stored;", "yield return this.@Number;")
            .And.NotContain("this.@Computed").And.NotContain("ManualValue");
    }

    [Fact(DisplayName = "Adding a stored property updates generated equality and component names")]
    public void Generate_AfterPropertyIsAdded_UpdatesBothMethods()
    {
        // Arrange
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int First { get; } = 1;
            }
            """);
        var driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var added = CSharpSyntaxTree.ParseText("""
            public sealed partial class Value
            {
                public string Second { get; } = "second";
            }
            """, ParseOptions, "Source1.cs", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation.AddSyntaxTrees(added), out var output, out _,
            TestContext.Current.CancellationToken);

        // Assert
        AssertCompiles(output);
        var result = driver.GetRunResult();
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().Contain("yield return this.@First;", "yield return this.@Second;", "[\"First\", \"Second\"]");
    }

    [Theory(DisplayName = "Adding a manual override removes only its generated implementation")]
    [InlineData("protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => [Number];", "GetEqualityComponents()")]
    [InlineData("public override string ToString() => \"manual\";", "public override string ToString()")]
    public void Generate_AfterManualOverrideIsAdded_RemovesConflictingGeneratedMethod(string method, string generatedSignature)
    {
        // Arrange
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int Number { get; } = 1;
            }
            """);
        var driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var added = CSharpSyntaxTree.ParseText("public sealed partial class Value { " + method + " }", ParseOptions,
            "Manual.cs", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation.AddSyntaxTrees(added), out var output, out _,
            TestContext.Current.CancellationToken);

        // Assert
        AssertCompiles(output);
        var result = driver.GetRunResult();
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Single().SourceText.ToString().Should().NotContain(generatedSignature);
    }

    [Fact(DisplayName = "Generator reuses unchanged output after unrelated source edits")]
    public void Generate_AfterUnrelatedEdit_ReusesGeneratedOutput()
    {
        // Arrange
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int Number { get; } = 1;
            }
            """);
        var driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var added = CSharpSyntaxTree.ParseText("class Unrelated { }", ParseOptions,
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(added), TestContext.Current.CancellationToken);

        // Assert
        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps.Values
            .SelectMany(steps => steps).SelectMany(step => step.Outputs);
        outputs.Should().NotBeEmpty().And.OnlyContain(output => output.Reason == IncrementalStepRunReason.Cached);
    }

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        return CSharpCompilation.Create("ValueObjectGeneratorTest",
            sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, ParseOptions, $"Source{index}.cs")),
            References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static GeneratorDriver CreateDriver()
    {
        return CSharpGeneratorDriver.Create([new ValueObjectGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    private static (GeneratorDriverRunResult Result, Compilation Output) Generate(CSharpCompilation compilation)
    {
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out _,
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

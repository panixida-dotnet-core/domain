using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using PANiXiDA.Core.Domain.Generators.ValueObjects;
using PANiXiDA.Core.Domain.ValueObjects;

namespace PANiXiDA.Core.Domain.UnitTests.ValueObjects;

public sealed class ValueObjectGeneratorTests
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
    private static readonly MetadataReference[] References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ValueObject).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
    ];

    [Fact(DisplayName = "Generator ignores a same-named value object from an extern-alias-only assembly")]
    public void Generate_WithForeignValueObject_DoesNotEmitSource()
    {
        var foreignCompilation = CreateCompilation("""
            namespace PANiXiDA.Core.Domain.ValueObjects;
            public abstract class ValueObject;
            """).WithAssemblyName("ForeignDomain");
        using var stream = new MemoryStream();
        foreignCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.Should().BeTrue();
        var compilation = CreateCompilation("""
            extern alias foreign;
            public sealed partial class Value : foreign::PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public string Text { get; } = "test";
            }
            """).AddReferences(MetadataReference.CreateFromImage(stream.ToArray()).WithAliases(["foreign"]));

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
        AssertCompiles(output);
    }

    [Fact(DisplayName = "Generator handles compilations without ValueObject")]
    public void Generate_WithoutValueObjectReference_DoesNotEmitSource()
    {
        var compilation = CreateCompilation("public class Unrelated : System.Exception;")
            .WithReferences(References.Where(reference => reference.Display != typeof(ValueObject).Assembly.Location));

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
        AssertCompiles(output);
    }

    [Theory(DisplayName = "Generated text requires only equality components from the base value object")]
    [InlineData(false)]
    [InlineData(true)]
    public void Generate_WithoutBaseFormattingHelper_EmitsCompilableOverrides(bool manualEquality)
    {
        string equality = manualEquality
            ? "protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => [Value.Trim()];"
            : string.Empty;
        var compilation = CreateCompilation($$"""
            namespace PANiXiDA.Core.Domain.ValueObjects
            {
                public abstract class ValueObject
                {
                    protected abstract System.Collections.Generic.IEnumerable<object?> GetEqualityComponents();
                }
            }

            public sealed partial class Email(string value) : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public string Value { get; } = value;
                {{equality}}
            }
            """).WithReferences(References.Where(reference => reference.Display != typeof(ValueObject).Assembly.Location));

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().ContainSingle();
        output.GetTypeByMetadataName("Email")!.GetMembers("ToString").Should().ContainSingle();
    }

    [Theory(DisplayName = "Generator preserves each manually declared method independently")]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Generate_WithManualMethods_EmitsOnlyMissingOverrides(bool manualEquality, bool manualToString)
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        var sources = result.Results.Single().GeneratedSources;
        if (manualEquality && manualToString)
        {
            sources.Should().BeEmpty();
            return;
        }

        var generated = sources.Should().ContainSingle().Subject;
        var methodNames = generated.SyntaxTree.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<MethodDeclarationSyntax>().Select(method => method.Identifier.ValueText).ToArray();
        methodNames.Contains("GetEqualityComponents").Should().Be(!manualEquality);
        methodNames.Contains("ToString").Should().Be(!manualToString);
        string source = generated.SourceText.ToString();
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

        var (result, output) = Generate(compilation);

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
    [InlineData("public partial class Value", "public partial int Number { get; } public partial int Number => 1;", "PANVO003", "Value")]
    [InlineData("public partial class Value", "", "PANVO003", "Value")]
    public void Generate_WithUnsupportedDeclaration_ReportsDiagnostic(string declaration, string members, string id, string typeName)
    {
        string source = declaration + " : PANiXiDA.Core.Domain.ValueObjects.ValueObject { " + members + " }"
            + (typeName == "Container" ? " }" : string.Empty);

        var (result, _) = Generate(CreateCompilation(source));

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(id);
        diagnostic.GetMessage().Should().Contain(typeName);
        diagnostic.Location.GetLineSpan().Path.Should().Be("Source0.cs");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Theory(DisplayName = "Implemented partial properties with computed getters are excluded from generated equality and text")]
    [InlineData("=> throw new System.InvalidOperationException();")]
    [InlineData("{ get => throw new System.InvalidOperationException(); }")]
    [InlineData("{ get { throw new System.InvalidOperationException(); } }")]
    public void Generate_WithImplementedPartialProperty_ExcludesComputedGetter(string implementation)
    {
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int Stored { get; } = 1;
                public partial int Computed { get; }
            }
            """, $$"""
            public sealed partial class Value
            {
                public partial int Computed {{implementation}}
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().Contain("yield return this.@Stored;").And.NotContain("Computed");
    }

    [Fact(DisplayName = "Existing non-partial value objects and abstract bases need no migration")]
    public void Generate_WithManualNonPartialAndUnrelatedTypes_DoesNotEmitSource()
    {
        var compilation = CreateCompilation("""
            public class Unrelated : System.Exception { }
            public abstract class AbstractValue : PANiXiDA.Core.Domain.ValueObjects.ValueObject { }
            public sealed class ManualValue : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => [];
            }
            """);

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Generator combines partial declarations and ignores method overloads")]
    public void Generate_WithMultiplePartsAndOverloads_EmitsOneCompleteType()
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().ContainAll(
            "yield return this.@First;",
            "yield return this.@Second;",
            "public override string ToString()");
    }

    [Fact(DisplayName = "Generator recognizes inherited auto-properties in referenced assemblies")]
    public void Generate_WithReferencedAbstractBase_IncludesStoredPropertiesOnly()
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().ContainAll("yield return this.@BaseValue;", "yield return this.@Number;");
        source.Should().NotContain(".@Computed");
    }

    [Fact(DisplayName = "Generated overrides inherited from a referenced assembly can be extended with derived state")]
    public void Generate_WithReferencedGeneratedBase_IncludesBaseAndDerivedComponents()
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().ContainAll(
            "yield return this.@BaseValue;",
            "yield return this.@Number;",
            "public override string ToString()");
    }

    [Theory(DisplayName = "Inherited manual overrides are preserved regardless of unrelated attributes")]
    [InlineData("")]
    [InlineData("[System.Diagnostics.DebuggerStepThrough]")]
    [InlineData("[System.CodeDom.Compiler.GeneratedCode(\"OtherGenerator\", \"1.0\")]")]
    [InlineData("[System.CodeDom.Compiler.GeneratedCode(null!, \"1.0\")]")]
    public void Generate_WithInheritedManualMethods_PreservesBaseImplementations(string attribute)
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact(DisplayName = "Inherited abstract overrides are implemented by the generator")]
    public void Generate_WithInheritedAbstractMethods_ImplementsBothOverrides()
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().ContainAll("yield return this.@Number;", "public override string ToString()");
    }

    [Fact(DisplayName = "Generator ignores unimplemented abstract properties while a type is being edited")]
    public void Generate_WithUnimplementedAbstractProperty_UsesOnlyStoredProperties()
    {
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

        var (result, output) = Generate(compilation);

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
        var compilation = CreateCompilation($$"""
            public class ManualBase : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                {{attribute}}
                protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => ["manual"];
                public override string ToString() => "manual";
            }
            public sealed partial class DerivedValue : ManualBase;
            """);

        var (result, output) = Generate(compilation);

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Should().NotBeEmpty().And.OnlyContain(item => item.Id == diagnosticId);
    }

    [Fact(DisplayName = "Missing metadata attribute dependencies do not change inherited methods or property selection")]
    public void Generate_WithUnavailableAttributeAssembly_PreservesManualMethodsAndStoredProperties()
    {
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

        var (result, output) = Generate(compilation);

        AssertCompiles(output);
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
        source.Should().ContainAll("yield return this.@Stored;", "yield return this.@Number;")
            .And.NotContain("this.@Computed").And.NotContain("ManualValue");
    }

    [Fact(DisplayName = "Adding a stored property updates generated equality and component names")]
    public void Generate_AfterPropertyIsAdded_UpdatesBothMethods()
    {
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

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation.AddSyntaxTrees(added), out var output, out _,
            TestContext.Current.CancellationToken);

        AssertCompiles(output);
        var result = driver.GetRunResult();
        result.Diagnostics.Should().BeEmpty();
        string source = result.Results.Single().GeneratedSources.Single().SourceText.ToString();
        source.Should().ContainAll(
            "yield return this.@First;",
            "yield return this.@Second;",
            "[\"First\", \"Second\"]");
    }

    [Theory(DisplayName = "Adding a manual override removes only its generated implementation")]
    [InlineData("protected override System.Collections.Generic.IEnumerable<object?> GetEqualityComponents() => [Number];", "GetEqualityComponents")]
    [InlineData("public override string ToString() => \"manual\";", "ToString")]
    public void Generate_AfterManualOverrideIsAdded_RemovesConflictingGeneratedMethod(
        string method,
        string methodName)
    {
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int Number { get; } = 1;
            }
            """);
        var driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var added = CSharpSyntaxTree.ParseText("public sealed partial class Value { " + method + " }", ParseOptions,
            "Manual.cs", cancellationToken: TestContext.Current.CancellationToken);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation.AddSyntaxTrees(added), out var output, out _,
            TestContext.Current.CancellationToken);

        AssertCompiles(output);
        var result = driver.GetRunResult();
        result.Diagnostics.Should().BeEmpty();
        var methodNames = result.Results.Single().GeneratedSources.Single().SyntaxTree
            .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Select(declaration => declaration.Identifier.ValueText);
        methodNames.Should().NotContain(methodName);
    }

    [Fact(DisplayName = "Generator reuses unchanged output after unrelated source edits")]
    public void Generate_AfterUnrelatedEdit_ReusesGeneratedOutput()
    {
        var compilation = CreateCompilation("""
            public sealed partial class Value : PANiXiDA.Core.Domain.ValueObjects.ValueObject
            {
                public int Number { get; } = 1;
            }
            """);
        var driver = CreateDriver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        var added = CSharpSyntaxTree.ParseText("class Unrelated { }", ParseOptions,
            cancellationToken: TestContext.Current.CancellationToken);

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(added), TestContext.Current.CancellationToken);

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

    private static CSharpGeneratorDriver CreateDriver()
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

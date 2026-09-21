using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests.Identifiers;

public sealed partial class StronglyTypedIdGenerationTests
{
    [Theory(DisplayName = "Generated record identifier text matches its Guid through all call sites")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("74a39a91-faba-4655-b5a0-4302017ff13e")]
    public void ToString_WithRecordIdentifier_ReturnsGuidText(string text)
    {
        var value = Guid.Parse(text);
        var id = new RecordId(value);
        object boxed = id;
        IStronglyTypedId contract = id;

        id.ToString().Should().Be(text);
        boxed.ToString().Should().Be(text);
        contract.ToString().Should().Be(text);
        $"{id}".Should().Be(text);
        (id == new RecordId(value)).Should().BeTrue();
    }

    [Fact(DisplayName = "Generated struct identifier text uses the current Guid")]
    public void ToString_WithMutableStruct_UsesCurrentValue()
    {
        var id = new MutableId();
        id.ToString().Should().Be(Guid.Empty.ToString());

        id.Value = Guid.Parse("74a39a91-faba-4655-b5a0-4302017ff13e");

        id.ToString().Should().Be(id.Value.ToString());
    }

    [Fact(DisplayName = "Manually declared identifier text is preserved")]
    public void ToString_WithManualOverride_PreservesCustomFormat()
    {
        var value = Guid.Parse("74a39a91-faba-4655-b5a0-4302017ff13e");
        var id = new CustomId(value);

        id.ToString().Should().Be(value.ToString("N"));
    }

    [Fact(DisplayName = "Non-partial record identifiers retain compiler-generated text")]
    public void ToString_WithNonPartialIdentifier_PreservesRecordFormat()
    {
        var value = Guid.Parse("74a39a91-faba-4655-b5a0-4302017ff13e");
        var id = new ExistingId(value);

        id.ToString().Should().Be($"ExistingId {{ Value = {value} }}");
    }

    [Fact(DisplayName = "Explicit identifier implementations format the interface Guid instead of a same-named property")]
    public void ToString_WithExplicitImplementation_UsesContractValue()
    {
        var value = Guid.Parse("74a39a91-faba-4655-b5a0-4302017ff13e");
        var id = new ExplicitId(value);

        id.ToString().Should().Be(value.ToString());
    }

    [Fact(DisplayName = "Identifier formatting supports explicit interface implementations on ref structs")]
    public void ToString_WithRefStruct_UsesContractValue()
    {
        var value = Guid.Parse("74a39a91-faba-4655-b5a0-4302017ff13e");
        var id = new RefId(value);

        id.ToString().Should().Be(value.ToString());
    }

    [Fact(DisplayName = "Identifiers implementing a derived interface receive generated text")]
    public void ToString_WithDerivedInterface_ReturnsGuidText()
    {
        var value = Guid.Parse("74a39a91-faba-4655-b5a0-4302017ff13e");
        var id = new DerivedId(value);

        id.ToString().Should().Be(value.ToString());
    }

    private readonly partial record struct RecordId(Guid Value) : IStronglyTypedId;

    private partial struct MutableId : IStronglyTypedId
    {
        public Guid Value { get; set; }
    }

    private readonly partial record struct CustomId(Guid Value) : IStronglyTypedId
    {
        public override string ToString() => Value.ToString("N");
    }

    private readonly record struct ExistingId(Guid Value) : IStronglyTypedId;

    private readonly partial record struct ExplicitId(Guid Original) : IStronglyTypedId
    {
        Guid IStronglyTypedId.Value => Original;
        public string Value => "Unrelated " + Original;
    }

    private readonly ref partial struct RefId(Guid value) : IStronglyTypedId
    {
        Guid IStronglyTypedId.Value => value;
    }

    private interface IDerivedId : IStronglyTypedId;

    private readonly partial record struct DerivedId(Guid Value) : IDerivedId;
}

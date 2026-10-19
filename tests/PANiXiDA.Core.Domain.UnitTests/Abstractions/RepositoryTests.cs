using PANiXiDA.Core.Domain.Abstractions;
using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests.Abstractions;

public sealed class RepositoryTests
{
    [Theory(DisplayName = "Repository writes synchronously accept only the aggregate")]
    [InlineData("Add")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public void WriteMethods_ExposeSynchronousContract(string methodName)
    {
        // Arrange
        Type repositoryType = typeof(IRepository<,>);
        Type aggregateType = repositoryType.GetGenericArguments()[1];

        // Act
        var method = repositoryType.GetMethod(methodName);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
        method.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(aggregateType);
        repositoryType.GetMethod($"{methodName}Async").Should().BeNull();
    }

    [Fact(DisplayName = "Repository identifier requires the strongly typed identifier contract")]
    public void IdentifierTypeParameter_RequiresStronglyTypedIdentifierContract()
    {
        Type identifierTypeParameter = typeof(IRepository<,>).GetGenericArguments()[0];

        Type[] constraints = identifierTypeParameter.GetGenericParameterConstraints();

        constraints.Should().Contain(typeof(IStronglyTypedId));
    }
}

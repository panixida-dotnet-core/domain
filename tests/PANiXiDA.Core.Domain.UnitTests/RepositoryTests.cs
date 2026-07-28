using PANiXiDA.Core.Domain.Abstractions;
using PANiXiDA.Core.Domain.Identifiers;

namespace PANiXiDA.Core.Domain.UnitTests;

public sealed class RepositoryTests
{
    [Fact(DisplayName = "Repository identifier requires the strongly typed identifier contract")]
    public void IdentifierTypeParameter_RequiresStronglyTypedIdentifierContract()
    {
        Type identifierTypeParameter = typeof(IRepository<,>).GetGenericArguments()[0];

        Type[] constraints = identifierTypeParameter.GetGenericParameterConstraints();

        constraints.Should().Contain(typeof(IStronglyTypedId));
    }
}

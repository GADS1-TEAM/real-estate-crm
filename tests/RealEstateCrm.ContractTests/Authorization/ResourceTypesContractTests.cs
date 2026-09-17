using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.ContractTests.Authorization;

public class ResourceTypesContractTests
{
    [Theory]
    [InlineData(nameof(ResourceTypes.User), "user")]
    [InlineData(nameof(ResourceTypes.Catalog), "catalog")]
    [InlineData(nameof(ResourceTypes.Party), "party")]
    public void Declared_resource_type_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(ResourceTypes).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }
}

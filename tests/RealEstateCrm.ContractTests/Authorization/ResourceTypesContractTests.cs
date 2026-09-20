using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.ContractTests.Authorization;

public class ResourceTypesContractTests
{
    [Theory]
    [InlineData(nameof(ResourceTypes.User), "user")]
    [InlineData(nameof(ResourceTypes.Catalog), "catalog")]
    [InlineData(nameof(ResourceTypes.Party), "party")]
    [InlineData(nameof(ResourceTypes.Property), "property")]
    [InlineData(nameof(ResourceTypes.Listing), "listing")]
    [InlineData(nameof(ResourceTypes.Requirement), "requirement")]
    [InlineData(nameof(ResourceTypes.Captation), "captation")]
    [InlineData(nameof(ResourceTypes.Match), "match")]
    public void Declared_resource_type_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(ResourceTypes).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }
}

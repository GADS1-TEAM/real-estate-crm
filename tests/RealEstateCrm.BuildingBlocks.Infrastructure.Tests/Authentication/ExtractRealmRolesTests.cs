using RealEstateCrm.BuildingBlocks.Infrastructure.Authentication;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Authentication;

public class ExtractRealmRolesTests
{
    [Fact]
    public void Extracts_roles_from_realm_access_json()
    {
        const string realmAccessJson = """{"roles":["Vendedor","ResponsableComercial"]}""";

        var roles = ClaimsPrincipalAuthenticationPort.ExtractRealmRoles(realmAccessJson);

        Assert.Equal(["Vendedor", "ResponsableComercial"], roles);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("""{"roles": "not-an-array"}""")]
    public void Returns_empty_for_missing_or_malformed_claim(string? realmAccessJson)
    {
        var roles = ClaimsPrincipalAuthenticationPort.ExtractRealmRoles(realmAccessJson);

        Assert.Empty(roles);
    }
}

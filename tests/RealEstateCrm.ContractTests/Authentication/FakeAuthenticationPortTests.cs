using RealEstateCrm.TestSupport.Authentication;

namespace RealEstateCrm.ContractTests.Authentication;

public class FakeAuthenticationPortTests
{
    [Fact]
    public async Task ForUser_resolves_the_configured_user_with_roles()
    {
        var port = FakeAuthenticationPort.ForUser("Ana Vendedora", "ana@demo.test", "Vendedor", "ResponsableComercial");

        var user = await port.ResolveCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal("Ana Vendedora", user.DisplayName);
        Assert.Equal("ana@demo.test", user.Email);
        Assert.Contains("Vendedor", user.Roles);
        Assert.Contains("ResponsableComercial", user.Roles);
    }

    [Fact]
    public async Task Anonymous_resolves_no_user()
    {
        var port = FakeAuthenticationPort.Anonymous();

        var user = await port.ResolveCurrentUserAsync();

        Assert.Null(user);
    }
}

using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.TestSupport.Authorization;

namespace RealEstateCrm.ContractTests.Authorization;

public class FakeAuthorizationPortTests
{
    [Fact]
    public async Task AllowAll_grants_any_permission()
    {
        var port = FakeAuthorizationPort.AllowAll();

        var decision = await port.EvaluateAsync(
            Guid.NewGuid(), Permissions.PartiesWrite, ResourceTypes.Party, resourceId: Guid.NewGuid());

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task DenyAll_denies_any_permission_with_the_given_reason()
    {
        var port = FakeAuthorizationPort.DenyAll(DenyReasons.UserInactive);

        var decision = await port.EvaluateAsync(
            Guid.NewGuid(), Permissions.CatalogsRead, ResourceTypes.Catalog, resourceId: null);

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.UserInactive, decision.ReasonCode);
    }

    [Fact]
    public async Task DenyAll_defaults_to_permission_not_granted()
    {
        var port = FakeAuthorizationPort.DenyAll();

        var decision = await port.EvaluateAsync(Guid.NewGuid(), Permissions.UsersManage, ResourceTypes.User, null);

        Assert.Equal(DenyReasons.PermissionNotGranted, decision.ReasonCode);
    }

    [Fact]
    public async Task AllowingOnly_grants_the_listed_permissions_and_denies_the_rest()
    {
        var port = FakeAuthorizationPort.AllowingOnly(Permissions.PartiesRead);

        var allowed = await port.EvaluateAsync(Guid.NewGuid(), Permissions.PartiesRead, ResourceTypes.Party, null);
        var denied = await port.EvaluateAsync(Guid.NewGuid(), Permissions.PartiesWrite, ResourceTypes.Party, null);

        Assert.True(allowed.Allowed);
        Assert.False(denied.Allowed);
        Assert.Equal(DenyReasons.PermissionNotGranted, denied.ReasonCode);
    }
}

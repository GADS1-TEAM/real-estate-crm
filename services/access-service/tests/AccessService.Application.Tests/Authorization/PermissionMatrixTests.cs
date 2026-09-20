using AccessService.Application.Authorization;
using AccessService.Domain;
using RealEstateCrm.Contracts.Authorization;

namespace AccessService.Application.Tests.Authorization;

public class PermissionMatrixTests
{
    [Theory]
    [InlineData(RoleCodes.Administrador, Permissions.UsersManage, true)]
    [InlineData(RoleCodes.Administrador, Permissions.CatalogsManage, true)]
    [InlineData(RoleCodes.Administrador, Permissions.PartiesAssignResponsible, true)]
    [InlineData(RoleCodes.Vendedor, Permissions.UsersManage, false)]
    [InlineData(RoleCodes.Vendedor, Permissions.PartiesWrite, true)]
    [InlineData(RoleCodes.Vendedor, Permissions.PartiesChangeCommercialStatus, false)]
    [InlineData(RoleCodes.Vendedor, Permissions.PartiesAssignResponsible, false)]
    [InlineData(RoleCodes.ResponsableComercial, Permissions.PartiesChangeCommercialStatus, true)]
    [InlineData(RoleCodes.ResponsableComercial, Permissions.PartiesAssignResponsible, true)]
    [InlineData(RoleCodes.ResponsableComercial, Permissions.UsersManage, false)]
    [InlineData(RoleCodes.ResponsableComercial, Permissions.CatalogsManage, false)]
    public void RoleHasPermission_matches_the_published_matrix(string roleCode, string permission, bool expected)
    {
        Assert.Equal(expected, PermissionMatrix.RoleHasPermission(roleCode, permission));
    }

    [Fact]
    public void RoleHasPermission_returns_false_for_unknown_role()
    {
        Assert.False(PermissionMatrix.RoleHasPermission("NoExiste", Permissions.UsersRead));
    }

    [Fact]
    public void AsDictionary_covers_the_three_mandatory_roles()
    {
        var matrix = PermissionMatrix.AsDictionary();

        Assert.Equal(RoleCodes.All.Count, matrix.Count);
        Assert.All(RoleCodes.All, role => Assert.True(matrix.ContainsKey(role)));
    }

    [Fact]
    public void All_three_roles_can_read_users_and_catalogs()
    {
        foreach (var role in RoleCodes.All)
        {
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.UsersRead));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.CatalogsRead));
        }
    }

    [Fact]
    public void All_three_roles_have_expected_wave3_permissions()
    {
        foreach (var role in RoleCodes.All)
        {
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.PropertiesRead));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.PropertiesWrite));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.ListingsRead));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.ListingsWrite));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.RequirementsRead));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.RequirementsWrite));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.CaptationsRead));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.CaptationsWrite));
            Assert.True(PermissionMatrix.RoleHasPermission(role, Permissions.MatchesRead));
        }
    }
}

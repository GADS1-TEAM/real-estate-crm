using AccessService.Domain;

namespace AccessService.Application.Tests.Domain;

public class RoleAssignmentTests
{
    [Fact]
    public void Reassign_replaces_the_role_and_keeps_the_same_user_id()
    {
        var userId = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var assignment = RoleAssignment.Create(userId, RoleCodes.Vendedor, admin, DateTimeOffset.UtcNow);

        var reassigned = assignment.Reassign(RoleCodes.Administrador, admin, DateTimeOffset.UtcNow);

        Assert.Equal(userId, reassigned.UserId);
        Assert.Equal(RoleCodes.Administrador, reassigned.RoleCode);
        Assert.Equal(assignment.Version + 1, reassigned.Version);
    }

    [Theory]
    [InlineData(RoleCodes.Administrador, true)]
    [InlineData(RoleCodes.Vendedor, true)]
    [InlineData(RoleCodes.ResponsableComercial, true)]
    [InlineData("Otro", false)]
    public void IsValid_only_accepts_the_three_mandatory_roles(string roleCode, bool expected)
    {
        Assert.Equal(expected, RoleCodes.IsValid(roleCode));
    }
}

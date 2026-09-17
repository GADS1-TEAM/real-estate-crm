using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.ContractTests.Authorization;

/// <summary>
/// Congela los literales de <see cref="Permissions"/>: son el contrato que van a consumir
/// V2-ACL-001, V2-CAT-001 y V2-PTY-001. Un cambio accidental de valor acá rompe a esas tasks en
/// silencio si no está testeado.
/// </summary>
public class PermissionsContractTests
{
    [Theory]
    [InlineData(nameof(Permissions.UsersRead), "users.read")]
    [InlineData(nameof(Permissions.UsersManage), "users.manage")]
    [InlineData(nameof(Permissions.CatalogsRead), "catalogs.read")]
    [InlineData(nameof(Permissions.CatalogsManage), "catalogs.manage")]
    [InlineData(nameof(Permissions.PartiesRead), "parties.read")]
    [InlineData(nameof(Permissions.PartiesWrite), "parties.write")]
    [InlineData(nameof(Permissions.PartiesChangeCommercialStatus), "parties.change_commercial_status")]
    [InlineData(nameof(Permissions.PartiesAssignResponsible), "parties.assign_responsible")]
    public void Declared_permission_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(Permissions).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }

    [Fact]
    public void Exposes_exactly_the_eight_wave_2_permissions()
    {
        var values = typeof(Permissions)
            .GetFields()
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.Equal(8, values.Count);
        Assert.Equal(values.Count, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("users.read")]
    [InlineData("users.manage")]
    [InlineData("catalogs.read")]
    [InlineData("catalogs.manage")]
    [InlineData("parties.read")]
    [InlineData("parties.write")]
    [InlineData("parties.change_commercial_status")]
    [InlineData("parties.assign_responsible")]
    public void Follows_the_resource_dot_action_snake_case_convention(string permission)
    {
        Assert.Matches("^[a-z]+\\.[a-z_]+$", permission);
    }
}

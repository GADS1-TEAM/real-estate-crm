using System.Text.Json;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Events.Access;

public class UserEventsV1ContractTests
{
    [Fact]
    public void UserCreatedV1_round_trips_and_serializes_as_camelCase()
    {
        var payload = new UserCreatedV1(Guid.NewGuid(), "Fabri", "fabri@crm-dev.local", "Active");

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<UserCreatedV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(payload, roundTripped);
        Assert.Contains("\"displayName\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void UserDeactivatedV1_carries_only_the_user_id()
    {
        var userId = Guid.NewGuid();
        var payload = new UserDeactivatedV1(userId);

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(userId.ToString(), document.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public void RoleAssignedV1_allows_a_null_previous_role_for_the_first_assignment()
    {
        var payload = new RoleAssignedV1(Guid.NewGuid(), PreviousRoleCode: null, "Vendedor", Guid.NewGuid());

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<RoleAssignedV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(payload, roundTripped);
        Assert.DoesNotContain("previousRoleCode", json, StringComparison.Ordinal); // WhenWritingNull omite el campo.
    }

    [Fact]
    public void RoleAssignedV1_carries_the_previous_role_on_a_reassignment()
    {
        var payload = new RoleAssignedV1(Guid.NewGuid(), "Vendedor", "Responsable Comercial", Guid.NewGuid());

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);

        Assert.Contains("\"previousRoleCode\":\"Vendedor\"", json, StringComparison.Ordinal);
    }
}

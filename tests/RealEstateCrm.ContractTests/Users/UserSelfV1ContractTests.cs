using System.Text.Json;
using RealEstateCrm.Contracts.Serialization;
using RealEstateCrm.Contracts.Users;

namespace RealEstateCrm.ContractTests.Users;

/// <summary>Contrato de <c>GET /api/v1/users/me</c> (V2-PTY-001): la forma exacta que consumen los owners.</summary>
public class UserSelfV1ContractTests
{
    [Fact]
    public void Serializes_with_the_frozen_camelCase_shape()
    {
        var userId = Guid.Parse("a1b2c3d4-0000-4000-8000-000000000001");

        var json = JsonSerializer.Serialize(new UserSelfV1(userId, "Active", "Vendedor"), RealEstateCrmJsonDefaults.Options);

        Assert.Equal(
            """{"userId":"a1b2c3d4-0000-4000-8000-000000000001","status":"Active","roleCode":"Vendedor"}""",
            json);
    }

    [Fact]
    public void Round_trips_a_user_without_role()
    {
        var self = new UserSelfV1(Guid.NewGuid(), "Active", RoleCode: null);

        var json = JsonSerializer.Serialize(self, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<UserSelfV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(self, roundTripped);
        Assert.False(JsonDocument.Parse(json).RootElement.TryGetProperty("roleCode", out _));
    }

    [Fact]
    public void Exposes_only_id_status_and_role_never_the_keycloak_subject()
    {
        var properties = typeof(UserSelfV1).GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.Equal(new[] { "RoleCode", "Status", "UserId" }, properties);
    }
}

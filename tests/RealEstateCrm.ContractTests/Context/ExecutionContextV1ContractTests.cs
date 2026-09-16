using System.Text.Json;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Context;

public class ExecutionContextV1ContractTests
{
    [Fact]
    public void Round_trips_actor_roles_permissions_and_correlation()
    {
        var original = new ExecutionContextV1(
            ActorId: Guid.NewGuid(),
            DisplayName: "Vendedor Demo",
            Email: "vendedor@demo.test",
            Roles: ["Vendedor"],
            Permissions: ["party.read", "party.write"],
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid());

        var json = JsonSerializer.Serialize(original, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<ExecutionContextV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.ActorId, roundTripped.ActorId);
        Assert.Equal(original.DisplayName, roundTripped.DisplayName);
        Assert.Equal(original.Email, roundTripped.Email);
        Assert.Equal(original.Roles, roundTripped.Roles);
        Assert.Equal(original.Permissions, roundTripped.Permissions);
        Assert.Equal(original.CorrelationId, roundTripped.CorrelationId);
        Assert.Equal(original.CausationId, roundTripped.CausationId);
    }

    [Fact]
    public void Does_not_declare_tenant_or_organization_fields()
    {
        var properties = typeof(ExecutionContextV1).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(properties, name =>
            name.Contains("Tenant", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Organization", StringComparison.OrdinalIgnoreCase));
    }
}

using System.Text.Json;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Events.Access;

/// <summary>
/// Este payload lo publica V2-PTY-001 (no V2-ACL-001, ver IAuthorizationPort remarks): el
/// contrato test asegura que la forma que ACL-001 publica hoy es la que PTY-001 va a poder
/// consumir sin sorpresas.
/// </summary>
public class ResponsibleAssignedV1ContractTests
{
    [Fact]
    public void Round_trips_with_a_previous_responsible()
    {
        var payload = new ResponsibleAssignedV1(
            ResourceTypes.Party,
            ResourceId: Guid.NewGuid(),
            PreviousResponsibleUserId: Guid.NewGuid(),
            NewResponsibleUserId: Guid.NewGuid(),
            AssignedByUserId: Guid.NewGuid());

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<ResponsibleAssignedV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(payload, roundTripped);
    }

    [Fact]
    public void First_assignment_has_no_previous_responsible()
    {
        var payload = new ResponsibleAssignedV1(
            ResourceTypes.Party,
            ResourceId: Guid.NewGuid(),
            PreviousResponsibleUserId: null,
            NewResponsibleUserId: Guid.NewGuid(),
            AssignedByUserId: Guid.NewGuid());

        var json = JsonSerializer.Serialize(payload, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);

        Assert.False(document.RootElement.TryGetProperty("previousResponsibleUserId", out _));
    }
}

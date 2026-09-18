using System.Text.Json;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Events.Party;
using RealEstateCrm.Contracts.Parties;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Parties;

/// <summary>Contratos públicos de party-service (V2-PTY-001): vocabulario, DTOs de lectura y eventos v1.</summary>
public class PartyContractsV1Tests
{
    [Fact]
    public void Vocabulary_matches_the_task_literals()
    {
        Assert.Equal("LEGAL_ENTITY", PartyKinds.LegalEntity);
        Assert.Equal("NATURAL_PERSON", PartyKinds.NaturalPerson);
        Assert.Equal(new[] { "POTENTIAL", "CUSTOMER", "INACTIVE", "DO_NOT_CONTACT" }, CommercialStatuses.All);
        Assert.Equal(new[] { "CONTACT_OF", "REPRESENTS" }, RelationshipTypes.All);
        Assert.Equal(
            new[] { "PROVISIONAL", "ACTIVE", "ALIASED", "INACTIVE", "RESTRICTED" },
            new[] { IdentityStatuses.Provisional, IdentityStatuses.Active, IdentityStatuses.Aliased, IdentityStatuses.Inactive, IdentityStatuses.Restricted });
    }

    [Fact]
    public void IdentityStatus_and_CommercialStatus_are_separate_fields_in_the_detail()
    {
        var names = typeof(PartyDetailV1).GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("IdentityStatus", names);
        Assert.Contains("CommercialStatus", names);
    }

    [Fact]
    public void Detail_round_trips_with_relationships_and_optional_fields_omitted()
    {
        var now = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
        var actor = Guid.NewGuid();
        var detail = new PartyDetailV1(
            Guid.NewGuid(), PartyKinds.NaturalPerson, "Ana Suárez", null, null, null, null, null, null, null, null, null,
            IdentityStatuses.Active, CommercialStatuses.Potential, Guid.NewGuid(), null, null, null,
            now, actor, now, actor, now, actor, 1,
            new[] { new PartyRelationshipV1(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RelationshipTypes.ContactOf, now, null, actor, Guid.NewGuid(), "Estudio Norte SA") });

        var json = JsonSerializer.Serialize(detail, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<PartyDetailV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.NotNull(roundTripped);
        Assert.Equal(detail.PartyId, roundTripped!.PartyId);
        Assert.Equal("ACTIVE", roundTripped.IdentityStatus);
        Assert.Equal("POTENTIAL", roundTripped.CommercialStatus);
        Assert.Single(roundTripped.Relationships);

        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("email", out _));
        Assert.True(document.RootElement.TryGetProperty("identityStatus", out _));
        Assert.True(document.RootElement.TryGetProperty("commercialStatus", out _));
    }

    [Fact]
    public void Event_payloads_round_trip()
    {
        var partyId = Guid.NewGuid();

        var registered = new PartyRegisteredV1(partyId, PartyKinds.LegalEntity, "Estudio Norte SA", IdentityStatuses.Active, CommercialStatuses.Potential, Guid.NewGuid(), "REFERIDO", 3);
        var updated = new PartyUpdatedV1(partyId, PartyKinds.LegalEntity, "Estudio Norte SA", new[] { "phone", "email" });
        var relationship = new PartyRelationshipCreatedV1(Guid.NewGuid(), Guid.NewGuid(), partyId, RelationshipTypes.ContactOf, DateTimeOffset.UtcNow);
        var status = new PartyCommercialStatusChangedV1(partyId, CommercialStatuses.Potential, CommercialStatuses.DoNotContact);
        var responsible = new ResponsibleAssignedV1(ResourceTypes.Party, partyId, null, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(registered, RoundTrip(registered));
        Assert.Equal(relationship, RoundTrip(relationship));
        Assert.Equal(status, RoundTrip(status));
        Assert.Equal(responsible, RoundTrip(responsible));
        Assert.Equal(new[] { "phone", "email" }, RoundTrip(updated)!.ChangedFields);
    }

    [Fact]
    public void Event_payloads_carry_no_sensitive_contact_data()
    {
        var forbidden = new[] { "email", "phone", "taxidentifier", "identitydocument", "address", "notes" };

        var names = new[] { typeof(PartyRegisteredV1), typeof(PartyUpdatedV1), typeof(PartyRelationshipCreatedV1), typeof(PartyCommercialStatusChangedV1) }
            .SelectMany(t => t.GetProperties().Select(p => p.Name.ToLowerInvariant()))
            .ToArray();

        Assert.Empty(names.Intersect(forbidden));
    }

    private static T? RoundTrip<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, RealEstateCrmJsonDefaults.Options), RealEstateCrmJsonDefaults.Options);
}

using PartyService.Domain;

namespace PartyService.Application.Tests.Domain;

public class PartyDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private static PartyProfile Profile(string name = "Ana Suárez", string? email = null) =>
        new(name, null, null, null, null, null, email, null, null, null, null);

    private static Party NewParty(Guid? actor = null) =>
        Party.Register(PartyKind.NaturalPerson, Profile(), Guid.NewGuid(), null, null, actor ?? Guid.NewGuid(), Now);

    [Theory]
    [InlineData(PartyKind.LegalEntity)]
    [InlineData(PartyKind.NaturalPerson)]
    public void Register_starts_ACTIVE_and_POTENTIAL_and_never_ALIASED(PartyKind kind)
    {
        var party = Party.Register(kind, Profile(), Guid.NewGuid(), null, null, Guid.NewGuid(), Now);

        Assert.Equal(IdentityStatus.Active, party.IdentityStatus);
        Assert.Equal(CommercialStatus.Potential, party.CommercialStatus);
        Assert.NotEqual(IdentityStatus.Aliased, party.IdentityStatus);
        Assert.Equal(1, party.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_requires_a_display_name(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            Party.Register(PartyKind.NaturalPerson, Profile(name), Guid.NewGuid(), null, null, Guid.NewGuid(), Now));
    }

    [Fact]
    public void Register_stamps_actor_and_date_on_creation_and_on_the_commercial_status()
    {
        var actor = Guid.NewGuid();
        var party = NewParty(actor);

        Assert.Equal(actor, party.CreatedBy);
        Assert.Equal(Now, party.CreatedAt);
        Assert.Equal(actor, party.CommercialStatusChangedBy);
        Assert.Equal(Now, party.CommercialStatusChangedAt);
    }

    [Fact]
    public void ChangeCommercialStatus_leaves_identityStatus_untouched_and_stamps_actor_and_date()
    {
        var party = NewParty();
        var actor = Guid.NewGuid();
        var later = Now.AddHours(1);

        var doNotContact = party.ChangeCommercialStatus(CommercialStatus.DoNotContact, actor, later);

        Assert.Equal(IdentityStatus.Active, doNotContact.IdentityStatus);
        Assert.Equal(CommercialStatus.DoNotContact, doNotContact.CommercialStatus);
        Assert.Equal(actor, doNotContact.CommercialStatusChangedBy);
        Assert.Equal(later, doNotContact.CommercialStatusChangedAt);
        Assert.Equal(party.Version + 1, doNotContact.Version);

        // Independencia: ACTIVE + POTENTIAL y ACTIVE + DO_NOT_CONTACT son combinaciones válidas.
        Assert.Equal(party.IdentityStatus, doNotContact.IdentityStatus);
        Assert.NotEqual(party.CommercialStatus, doNotContact.CommercialStatus);
    }

    [Fact]
    public void ChangeCommercialStatus_to_the_same_status_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => NewParty().ChangeCommercialStatus(CommercialStatus.Potential, Guid.NewGuid(), Now));
    }

    [Fact]
    public void Logical_deactivation_keeps_every_field_and_is_reversible()
    {
        var party = NewParty();

        var inactive = party.ChangeCommercialStatus(CommercialStatus.Inactive, Guid.NewGuid(), Now.AddDays(1));
        var reactivated = inactive.ChangeCommercialStatus(CommercialStatus.Potential, Guid.NewGuid(), Now.AddDays(2));

        Assert.Equal(party.PartyId, inactive.PartyId);
        Assert.Equal(party.Profile, inactive.Profile);
        Assert.Equal(party.ResponsibleUserId, inactive.ResponsibleUserId);
        Assert.Equal(CommercialStatus.Potential, reactivated.CommercialStatus);
    }

    [Fact]
    public void UpdateData_changes_profile_only_and_reports_the_changed_field_names()
    {
        var party = NewParty();

        var updated = party.UpdateData(Profile("Ana María Suárez", "ana@correo.com"), "REFERIDO", 2, Guid.NewGuid(), Now.AddHours(1));

        Assert.Equal(party.PartyId, updated.PartyId);
        Assert.Equal(party.CommercialStatus, updated.CommercialStatus);
        Assert.Equal(party.ResponsibleUserId, updated.ResponsibleUserId);
        Assert.Equal(new[] { "displayName", "email", "originCode" }, party.ChangedFields(updated));
    }

    [Fact]
    public void AssignResponsible_to_the_current_responsible_is_rejected()
    {
        var party = NewParty();

        Assert.Throws<InvalidOperationException>(() => party.AssignResponsible(party.ResponsibleUserId!.Value, Guid.NewGuid(), Now));
    }

    [Fact]
    public void Relationship_is_current_until_it_has_an_end_date()
    {
        var relationship = PartyRelationship.Create(Guid.NewGuid(), Guid.NewGuid(), RelationshipType.ContactOf, Guid.NewGuid(), Now);

        Assert.True(relationship.IsCurrent(Now.AddYears(5)));
        Assert.False((relationship with { ValidTo = Now.AddDays(1) }).IsCurrent(Now.AddDays(2)));
    }
}

using System.Text.Json;
using PartyService.Application.Parties;
using PartyService.Application.Ports;
using PartyService.Application.Tests.Fakes;
using PartyService.Domain;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Events.Party;
using RealEstateCrm.Contracts.Parties;
using RealEstateCrm.Contracts.Serialization;
using static PartyService.Application.Tests.Fakes.PartyServiceTestHarness;

namespace PartyService.Application.Tests.Parties;

public class PartyManagementServiceTests
{
    private static async Task<PartyDomainException> AssertRejectedAsync(Func<Task> action, string errorCode, int status)
    {
        var exception = await Assert.ThrowsAsync<PartyDomainException>(action);
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(status, exception.HttpStatus);
        return exception;
    }

    private static EventEnvelopeV1<TPayload> Payload<TPayload>(RealEstateCrm.BuildingBlocks.Messaging.OutboxMessage message) =>
        JsonSerializer.Deserialize<EventEnvelopeV1<TPayload>>(message.EnvelopeJson, RealEstateCrmJsonDefaults.Options)!;

    // ---------- Alta (PTY-001 / PTY-003) ----------

    [Fact]
    public async Task CreateCompany_registers_a_LEGAL_ENTITY_ACTIVE_and_POTENTIAL_with_the_creator_as_responsible()
    {
        var h = new PartyServiceTestHarness();
        var context = h.Ctx(h.Vendedor);

        var company = await h.Service.CreateCompanyAsync(context, Data("Estudio Norte SA"));

        Assert.Equal(PartyKinds.LegalEntity, company.Kind);
        Assert.Equal(IdentityStatuses.Active, company.IdentityStatus);
        Assert.Equal(CommercialStatuses.Potential, company.CommercialStatus);
        Assert.Equal(h.Vendedor.UserId, company.ResponsibleUserId); // userId de access-service, no el sub.
        Assert.NotEqual(h.Vendedor.Subject, company.ResponsibleUserId);
        Assert.Equal(h.Vendedor.Subject, company.CreatedBy);

        var message = Assert.Single(h.Outbox.Messages);
        Assert.Equal("PartyRegistered", message.Name);
        Assert.Equal(h.Vendedor.Subject, message.ActorId);
        Assert.Equal(context.CorrelationId, message.CorrelationId);
        Assert.Equal(company.PartyId, message.AggregateId);
    }

    [Fact]
    public async Task CreateContact_registers_a_NATURAL_PERSON_and_it_can_exist_without_a_company()
    {
        var h = new PartyServiceTestHarness();

        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana Suárez"));
        var detail = await h.Service.GetContactDetailAsync(contact.PartyId);

        Assert.Equal(PartyKinds.NaturalPerson, detail.Kind);
        Assert.Empty(detail.Relationships);
        Assert.Empty(h.Relationships.All);
    }

    [Fact]
    public async Task Only_the_name_is_required_and_the_optional_fields_stay_missing()
    {
        var h = new PartyServiceTestHarness();

        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana Suárez"));

        Assert.Null(contact.Email);
        Assert.Null(contact.Phone);
        Assert.Null(contact.TaxIdentifier);
        Assert.Null(contact.OriginCode);
    }

    [Fact]
    public async Task Creation_never_deduplicates_by_tax_id_email_or_phone()
    {
        var h = new PartyServiceTestHarness();

        var first = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA", "contacto@norte.com", "+54 11 5555 0000", "30-11111111-1"));
        var second = await h.Service.CreateCompanyAsync(h.Ctx(h.OtherVendedor), Data("Estudio Norte SA", "contacto@norte.com", "+54 11 5555 0000", "30-11111111-1"));

        Assert.NotEqual(first.PartyId, second.PartyId);
        Assert.Equal(2, h.Parties.All.Count);
    }

    [Fact]
    public async Task Company_data_ignores_person_only_fields_and_contact_data_ignores_company_only_fields()
    {
        var h = new PartyServiceTestHarness();
        var input = new PartyDataInput("X", "Razón Social SA", "Ana", "Suárez", null, "DNI 1", null, null, null, "Textil", null, null);

        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), input);
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), input);

        Assert.Equal("Razón Social SA", company.LegalName);
        Assert.Equal("Textil", company.Industry);
        Assert.Null(company.GivenNames);
        Assert.Null(company.IdentityDocument);
        Assert.Equal("Ana", contact.GivenNames);
        Assert.Equal("DNI 1", contact.IdentityDocument);
        Assert.Null(contact.LegalName);
        Assert.Null(contact.Industry);
    }

    [Fact]
    public async Task Whitespace_only_optional_values_are_stored_as_missing_and_text_is_trimmed()
    {
        var h = new PartyServiceTestHarness();

        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("  Ana Suárez  ", email: "   ", phone: " +54 11 "));

        Assert.Equal("Ana Suárez", contact.DisplayName);
        Assert.Null(contact.Email);
        Assert.Equal("+54 11", contact.Phone);
    }

    [Theory]
    [InlineData(DenyReasons.UserPending)]
    [InlineData(DenyReasons.UserInactive)]
    public async Task A_pending_or_inactive_actor_cannot_create(string reason)
    {
        var h = new PartyServiceTestHarness();
        var stranger = new TestActor(Guid.NewGuid(), Guid.NewGuid(), null);
        h.UserDirectory.WithDeniedUser(stranger.Subject, reason);

        var exception = await AssertRejectedAsync(() => h.Service.CreateContactAsync(h.Ctx(stranger), Data("Ana")), ErrorCodes.Forbidden, 403);

        Assert.Equal(reason, exception.Message);
        Assert.Empty(h.Parties.All);
        Assert.Empty(h.Outbox.Messages);
    }

    // ---------- Origen (catálogo, D7) ----------

    [Fact]
    public async Task A_valid_originCode_is_stored_together_with_the_catalog_version_used()
    {
        var h = new PartyServiceTestHarness();
        h.Catalog.CatalogVersion = 7;

        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana", origin: "REFERIDO"));

        Assert.Equal("REFERIDO", contact.OriginCode);
        Assert.Equal(7, contact.OriginCatalogVersion);
    }

    [Theory]
    [InlineData("NO_EXISTE")]
    [InlineData("ORIGEN_VIEJO")] // existe pero está dado de baja: no seleccionable en altas nuevas.
    public async Task An_unknown_or_inactive_originCode_is_rejected(string origin)
    {
        var h = new PartyServiceTestHarness();

        await AssertRejectedAsync(() => h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana", origin: origin)), PartyErrorCodes.InvalidOrigin, 422);

        Assert.Empty(h.Parties.All);
    }

    [Fact]
    public async Task Updating_without_changing_the_origin_does_not_revalidate_it_nor_change_its_version()
    {
        var h = new PartyServiceTestHarness();
        h.Catalog.CatalogVersion = 3;
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana", origin: "REFERIDO"));
        var callsAfterCreate = h.Catalog.CallCount;
        h.Catalog.CatalogVersion = 9;

        var updated = await h.Service.UpdateContactAsync(h.Ctx(h.Vendedor), contact.PartyId, Data("Ana María", origin: "REFERIDO"));

        Assert.Equal(callsAfterCreate, h.Catalog.CallCount);
        Assert.Equal(3, updated.OriginCatalogVersion);
    }

    [Fact]
    public async Task Changing_the_origin_stores_the_new_catalog_version()
    {
        var h = new PartyServiceTestHarness();
        h.Catalog.CatalogVersion = 3;
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana", origin: "REFERIDO"));
        h.Catalog.CatalogVersion = 4;

        var updated = await h.Service.UpdateContactAsync(h.Ctx(h.Vendedor), contact.PartyId, Data("Ana", origin: "WHATSAPP"));

        Assert.Equal("WHATSAPP", updated.OriginCode);
        Assert.Equal(4, updated.OriginCatalogVersion);
    }

    // ---------- Edición y regla de propiedad (PTY-002 / PTY-004, D2) ----------

    [Fact]
    public async Task The_owner_updates_a_company_without_duplicating_identity_and_only_changed_field_names_are_published()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA"));

        var updated = await h.Service.UpdateCompanyAsync(h.Ctx(h.Vendedor), company.PartyId, Data("Estudio Norte SA", phone: "+54 11 5555 0000"));

        Assert.Equal(company.PartyId, updated.PartyId);
        Assert.Single(h.Parties.All);
        Assert.Equal("+54 11 5555 0000", updated.Phone);
        Assert.Equal(company.Version + 1, updated.Version);
        Assert.Equal(h.Vendedor.Subject, updated.UpdatedBy);

        var message = h.Outbox.Messages.Single(m => m.Name == "PartyUpdated");
        var payload = Payload<PartyUpdatedV1>(message).Payload;
        Assert.Equal(new[] { "phone" }, payload.ChangedFields);
        Assert.DoesNotContain("+54", message.EnvelopeJson); // sin el valor sensible en el evento.
    }

    [Fact]
    public async Task Updating_a_contact_keeps_its_relationship()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA"));
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        await h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, company.PartyId, RelationshipTypes.ContactOf);

        var updated = await h.Service.UpdateContactAsync(h.Ctx(h.Vendedor), contact.PartyId, Data("Ana Suárez"));

        var relationship = Assert.Single(updated.Relationships);
        Assert.Equal(company.PartyId, relationship.RelatedPartyId);
    }

    [Fact]
    public async Task An_update_that_changes_nothing_publishes_no_event_and_does_not_bump_the_version()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana", phone: "1"));

        var same = await h.Service.UpdateContactAsync(h.Ctx(h.Vendedor), contact.PartyId, Data("Ana", phone: "1"));

        Assert.Equal(contact.Version, same.Version);
        Assert.DoesNotContain(h.Outbox.Messages, m => m.Name == "PartyUpdated");
    }

    [Fact]
    public async Task A_vendedor_cannot_edit_a_party_where_they_are_not_the_responsible()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));

        var exception = await AssertRejectedAsync(
            () => h.Service.UpdateContactAsync(h.Ctx(h.OtherVendedor), contact.PartyId, Data("Hackeado")), ErrorCodes.Forbidden, 403);

        Assert.Equal(PartyErrorCodes.NotResponsibleReason, exception.Message);
        Assert.Equal("Ana", (await h.Service.GetContactDetailAsync(contact.PartyId)).DisplayName);
    }

    [Fact]
    public async Task A_responsable_comercial_who_is_not_the_responsible_cannot_edit_data_either()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA"));

        await AssertRejectedAsync(() => h.Service.UpdateCompanyAsync(h.Ctx(h.Responsable), company.PartyId, Data("Otro nombre")), ErrorCodes.Forbidden, 403);
    }

    [Fact]
    public async Task The_administrator_edits_any_party()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));

        var updated = await h.Service.UpdateContactAsync(h.Ctx(h.Admin), contact.PartyId, Data("Ana Suárez"));

        Assert.Equal("Ana Suárez", updated.DisplayName);
        Assert.Equal(h.Vendedor.UserId, updated.ResponsibleUserId); // editar no cambia el responsable.
    }

    [Fact]
    public async Task Updating_a_company_through_the_contact_operation_is_a_404()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA"));

        await AssertRejectedAsync(() => h.Service.UpdateContactAsync(h.Ctx(h.Vendedor), company.PartyId, Data("X")), PartyErrorCodes.PartyNotFound, 404);
        await AssertRejectedAsync(() => h.Service.UpdateCompanyAsync(h.Ctx(h.Vendedor), Guid.NewGuid(), Data("X")), PartyErrorCodes.PartyNotFound, 404);
    }

    // ---------- Relaciones (PTY-005) ----------

    [Fact]
    public async Task A_contact_related_to_a_company_shows_the_relationship_in_both_details()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA"));
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana Suárez"));

        var relationship = await h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, company.PartyId, RelationshipTypes.ContactOf);

        Assert.Equal(contact.PartyId, relationship.FromPartyId);
        Assert.Equal(company.PartyId, relationship.ToPartyId);
        Assert.Equal(h.Vendedor.Subject, relationship.CreatedBy);

        var companyDetail = await h.Service.GetCompanyDetailAsync(company.PartyId);
        var contactDetail = await h.Service.GetContactDetailAsync(contact.PartyId);

        Assert.Equal(contact.PartyId, Assert.Single(companyDetail.Relationships).RelatedPartyId);
        Assert.Equal("Ana Suárez", companyDetail.Relationships[0].RelatedPartyDisplayName);
        Assert.Equal(company.PartyId, Assert.Single(contactDetail.Relationships).RelatedPartyId);
        Assert.Equal("Estudio Norte SA", contactDetail.Relationships[0].RelatedPartyDisplayName);

        var message = h.Outbox.Messages.Single(m => m.Name == "PartyRelationshipCreated");
        Assert.Equal(relationship.RelationshipId, Payload<PartyRelationshipCreatedV1>(message).Payload.RelationshipId);
    }

    [Fact]
    public async Task A_contact_can_be_related_to_several_companies()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        var first = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Norte SA"));
        var second = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Sur SA"));

        await h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, first.PartyId, RelationshipTypes.ContactOf);
        await h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, second.PartyId, RelationshipTypes.Represents);

        Assert.Equal(2, (await h.Service.GetContactDetailAsync(contact.PartyId)).Relationships.Count);
    }

    [Fact]
    public async Task The_same_relationship_cannot_be_created_twice()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Norte SA"));
        await h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, company.PartyId, RelationshipTypes.ContactOf);

        await AssertRejectedAsync(
            () => h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, company.PartyId, RelationshipTypes.ContactOf),
            PartyErrorCodes.RelationshipAlreadyExists,
            409);
    }

    [Fact]
    public async Task Relating_requires_a_contact_and_a_company_and_a_valid_type()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        var otherContact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Beto"));
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Norte SA"));

        await AssertRejectedAsync(() => h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, otherContact.PartyId, RelationshipTypes.ContactOf), PartyErrorCodes.PartyNotFound, 404);
        await AssertRejectedAsync(() => h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), company.PartyId, company.PartyId, RelationshipTypes.ContactOf), PartyErrorCodes.PartyNotFound, 404);
        await AssertRejectedAsync(() => h.Service.RelateContactToCompanyAsync(h.Ctx(h.Vendedor), contact.PartyId, company.PartyId, "OWNER_OF"), PartyErrorCodes.InvalidRelationshipType, 422);
        Assert.Empty(h.Relationships.All);
    }

    [Fact]
    public async Task A_vendedor_cannot_relate_a_contact_they_do_not_own()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.OtherVendedor), Data("Norte SA"));

        await AssertRejectedAsync(
            () => h.Service.RelateContactToCompanyAsync(h.Ctx(h.OtherVendedor), contact.PartyId, company.PartyId, RelationshipTypes.ContactOf),
            ErrorCodes.Forbidden,
            403);
    }

    // ---------- Estado comercial y baja lógica (PTY-007) ----------

    [Fact]
    public async Task DO_NOT_CONTACT_does_not_delete_nor_hide_the_party_and_keeps_identityStatus_ACTIVE()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Responsable), Data("Ana"));
        var context = h.Ctx(h.Responsable);

        var changed = await h.Service.ChangeCommercialStatusAsync(context, contact.PartyId, CommercialStatuses.DoNotContact);

        Assert.Equal(CommercialStatuses.DoNotContact, changed.CommercialStatus);
        Assert.Equal(IdentityStatuses.Active, changed.IdentityStatus);
        Assert.Equal(contact.PartyId, (await h.Service.GetContactDetailAsync(contact.PartyId)).PartyId);
        Assert.Single(h.Parties.All);

        var search = await h.Service.SearchAsync(new PartySearchCriteria(null, null, null, null), 1, 20);
        Assert.Single(search.Items); // sigue apareciendo en la búsqueda.

        var message = h.Outbox.Messages.Single(m => m.Name == "PartyCommercialStatusChanged");
        var payload = Payload<PartyCommercialStatusChangedV1>(message).Payload;
        Assert.Equal(CommercialStatuses.Potential, payload.PreviousStatus);
        Assert.Equal(CommercialStatuses.DoNotContact, payload.NewStatus);
        Assert.Equal(h.Responsable.Subject, message.ActorId);
    }

    [Fact]
    public async Task Both_dimensions_are_independent_ACTIVE_POTENTIAL_versus_ACTIVE_DO_NOT_CONTACT()
    {
        var h = new PartyServiceTestHarness();
        var potential = await h.Service.CreateContactAsync(h.Ctx(h.Admin), Data("Ana"));
        var blocked = await h.Service.CreateContactAsync(h.Ctx(h.Admin), Data("Beto"));

        var blockedAfter = await h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), blocked.PartyId, CommercialStatuses.DoNotContact);

        Assert.Equal((IdentityStatuses.Active, CommercialStatuses.Potential), (potential.IdentityStatus, potential.CommercialStatus));
        Assert.Equal((IdentityStatuses.Active, CommercialStatuses.DoNotContact), (blockedAfter.IdentityStatus, blockedAfter.CommercialStatus));
    }

    [Fact]
    public async Task Logical_deactivation_records_actor_and_date_and_can_be_reactivated_without_losing_the_party()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Admin), Data("Norte SA"));
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Admin), Data("Ana"));
        await h.Service.RelateContactToCompanyAsync(h.Ctx(h.Admin), contact.PartyId, company.PartyId, RelationshipTypes.ContactOf);

        var inactive = await h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), company.PartyId, CommercialStatuses.Inactive);

        Assert.Equal(CommercialStatuses.Inactive, inactive.CommercialStatus);
        Assert.Equal(h.Admin.Subject, inactive.CommercialStatusChangedBy);
        Assert.True(inactive.CommercialStatusChangedAt >= company.CreatedAt);
        Assert.Single(inactive.Relationships); // conserva relaciones e historial.
        Assert.Single(h.Relationships.All);

        var reactivated = await h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), company.PartyId, CommercialStatuses.Potential);
        Assert.Equal(CommercialStatuses.Potential, reactivated.CommercialStatus);
    }

    [Theory]
    [InlineData(CommercialStatuses.Potential)] // ya lo es.
    public async Task Changing_to_the_current_status_is_a_conflict(string status)
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Admin), Data("Ana"));

        await AssertRejectedAsync(() => h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), contact.PartyId, status), PartyErrorCodes.CommercialStatusUnchanged, 409);
    }

    [Theory]
    [InlineData("ALIASED")] // es identityStatus, nunca un estado comercial.
    [InlineData("ACTIVE")]
    [InlineData("potential")]
    [InlineData("")]
    public async Task Only_the_four_commercial_statuses_are_accepted(string status)
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Admin), Data("Ana"));

        await AssertRejectedAsync(() => h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), contact.PartyId, status), PartyErrorCodes.InvalidCommercialStatus, 422);
    }

    [Fact]
    public async Task The_responsable_comercial_changes_status_only_on_parties_where_they_are_responsible()
    {
        var h = new PartyServiceTestHarness();
        var owned = await h.Service.CreateContactAsync(h.Ctx(h.Responsable), Data("Ana"));
        var foreign = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Beto"));

        var changed = await h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Responsable), owned.PartyId, CommercialStatuses.Customer);
        Assert.Equal(CommercialStatuses.Customer, changed.CommercialStatus);

        await AssertRejectedAsync(() => h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Responsable), foreign.PartyId, CommercialStatuses.Customer), ErrorCodes.Forbidden, 403);
    }

    [Fact]
    public async Task The_administrator_changes_the_status_of_any_party()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));

        var changed = await h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), contact.PartyId, CommercialStatuses.Customer);

        Assert.Equal(CommercialStatuses.Customer, changed.CommercialStatus);
    }

    // ---------- Responsable (PTY-007 / ASSIGN-001 / ASSIGN-002) ----------

    [Fact]
    public async Task Reassigning_the_responsible_publishes_ResponsibleAssigned_with_previous_new_and_actor_userId()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        var context = h.Ctx(h.Responsable);

        var reassigned = await h.Service.AssignResponsibleAsync(context, contact.PartyId, h.OtherVendedor.UserId);

        Assert.Equal(h.OtherVendedor.UserId, reassigned.ResponsibleUserId);

        var message = h.Outbox.Messages.Single(m => m.Name == "PartyResponsibleAssigned");
        var envelope = Payload<ResponsibleAssignedV1>(message);
        Assert.Equal(ResourceTypes.Party, envelope.Payload.ResourceType);
        Assert.Equal(contact.PartyId, envelope.Payload.ResourceId);
        Assert.Equal(h.Vendedor.UserId, envelope.Payload.PreviousResponsibleUserId);
        Assert.Equal(h.OtherVendedor.UserId, envelope.Payload.NewResponsibleUserId);
        Assert.Equal(h.Responsable.UserId, envelope.Payload.AssignedByUserId); // userId, no el sub.
        Assert.Equal(h.Responsable.Subject, envelope.ActorId);
        Assert.Equal(context.CorrelationId, envelope.CorrelationId);
    }

    [Fact]
    public async Task After_reassignment_the_new_responsible_can_edit_and_the_previous_one_cannot()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        await h.Service.AssignResponsibleAsync(h.Ctx(h.Admin), contact.PartyId, h.OtherVendedor.UserId);

        var edited = await h.Service.UpdateContactAsync(h.Ctx(h.OtherVendedor), contact.PartyId, Data("Ana Suárez"));
        Assert.Equal("Ana Suárez", edited.DisplayName);

        await AssertRejectedAsync(() => h.Service.UpdateContactAsync(h.Ctx(h.Vendedor), contact.PartyId, Data("Otra vez")), ErrorCodes.Forbidden, 403);
    }

    [Fact]
    public async Task An_actor_without_the_assign_permission_is_rejected_by_access_service_with_403()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        // Reemplaza el servicio por uno cuyo validador deniega el permiso (Vendedor no asigna).
        var service = NewServiceWithValidator(h, AuthorizationDecision.Deny(DenyReasons.PermissionNotGranted));

        var exception = await AssertRejectedAsync(() => service.AssignResponsibleAsync(h.Ctx(h.Vendedor), contact.PartyId, h.OtherVendedor.UserId), ErrorCodes.Forbidden, 403);

        Assert.Equal(DenyReasons.PermissionNotGranted, exception.Message);
        Assert.DoesNotContain(h.Outbox.Messages, m => m.Name == "PartyResponsibleAssigned");
    }

    [Theory]
    [InlineData(ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound)]
    [InlineData(ResponsibleAssignmentDenyReasons.ResponsibleUserInactive)]
    public async Task An_invalid_responsible_is_a_422_and_changes_nothing(string reason)
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));
        var service = NewServiceWithValidator(h, AuthorizationDecision.Deny(reason));

        var exception = await AssertRejectedAsync(() => service.AssignResponsibleAsync(h.Ctx(h.Admin), contact.PartyId, Guid.NewGuid()), PartyErrorCodes.ResponsibleInvalid, 422);

        Assert.Equal(reason, exception.Message);
        Assert.Equal(h.Vendedor.UserId, (await h.Service.GetContactDetailAsync(contact.PartyId)).ResponsibleUserId);
    }

    [Fact]
    public async Task Assigning_the_current_responsible_again_is_a_conflict()
    {
        var h = new PartyServiceTestHarness();
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));

        await AssertRejectedAsync(() => h.Service.AssignResponsibleAsync(h.Ctx(h.Admin), contact.PartyId, h.Vendedor.UserId), PartyErrorCodes.ResponsibleUnchanged, 409);
    }

    [Fact]
    public async Task Assigning_the_responsible_of_an_unknown_party_is_a_404()
    {
        var h = new PartyServiceTestHarness();

        await AssertRejectedAsync(() => h.Service.AssignResponsibleAsync(h.Ctx(h.Admin), Guid.NewGuid(), h.Vendedor.UserId), PartyErrorCodes.PartyNotFound, 404);
    }

    // ---------- Consultas (PTY-006) ----------

    [Fact]
    public async Task Detail_exposes_responsible_and_origin_and_separate_status_fields()
    {
        var h = new PartyServiceTestHarness();
        h.Catalog.CatalogVersion = 5;
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana", origin: "WHATSAPP"));

        var detail = await h.Service.GetContactDetailAsync(contact.PartyId);

        Assert.Equal(h.Vendedor.UserId, detail.ResponsibleUserId);
        Assert.Equal("WHATSAPP", detail.OriginCode);
        Assert.Equal(5, detail.OriginCatalogVersion);
        Assert.Equal(IdentityStatuses.Active, detail.IdentityStatus);
        Assert.Equal(CommercialStatuses.Potential, detail.CommercialStatus);
    }

    [Fact]
    public async Task Company_detail_of_a_contact_and_contact_detail_of_a_company_are_404_but_the_generic_detail_works()
    {
        var h = new PartyServiceTestHarness();
        var company = await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Norte SA"));
        var contact = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));

        await AssertRejectedAsync(() => h.Service.GetCompanyDetailAsync(contact.PartyId), PartyErrorCodes.PartyNotFound, 404);
        await AssertRejectedAsync(() => h.Service.GetContactDetailAsync(company.PartyId), PartyErrorCodes.PartyNotFound, 404);
        Assert.Equal(PartyKinds.LegalEntity, (await h.Service.GetPartyDetailAsync(company.PartyId)).Kind);
        Assert.Equal(PartyKinds.NaturalPerson, (await h.Service.GetPartyDetailAsync(contact.PartyId)).Kind);
    }

    [Fact]
    public async Task Search_filters_by_text_kind_status_and_responsible_and_paginates()
    {
        var h = new PartyServiceTestHarness();
        await h.Service.CreateCompanyAsync(h.Ctx(h.Vendedor), Data("Estudio Norte SA", "info@norte.com", taxId: "30-12345678-9"));
        var ana = await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana Suárez", phone: "+54 11 4444 1111"));
        await h.Service.CreateContactAsync(h.Ctx(h.OtherVendedor), Data("Beto Norte"));
        await h.Service.ChangeCommercialStatusAsync(h.Ctx(h.Admin), ana.PartyId, CommercialStatuses.DoNotContact);

        var byText = await h.Service.SearchAsync(new PartySearchCriteria("NORTE", null, null, null), 1, 20);
        Assert.Equal(new[] { "Beto Norte", "Estudio Norte SA" }, byText.Items.Select(i => i.DisplayName));

        Assert.Single((await h.Service.SearchAsync(new PartySearchCriteria("4444", null, null, null), 1, 20)).Items);
        Assert.Single((await h.Service.SearchAsync(new PartySearchCriteria("30-12345678", null, null, null), 1, 20)).Items);
        Assert.Equal(2, (await h.Service.SearchAsync(new PartySearchCriteria(null, PartyKind.NaturalPerson, null, null), 1, 20)).TotalCount);
        Assert.Equal("Ana Suárez", Assert.Single((await h.Service.SearchAsync(new PartySearchCriteria(null, null, CommercialStatus.DoNotContact, null), 1, 20)).Items).DisplayName);
        Assert.Equal(2, (await h.Service.SearchAsync(new PartySearchCriteria(null, null, null, h.Vendedor.UserId), 1, 20)).TotalCount);

        var firstPage = await h.Service.SearchAsync(new PartySearchCriteria(null, null, null, null), 1, 2);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(3, firstPage.TotalCount);
    }

    [Fact]
    public async Task Search_clamps_an_out_of_range_page_size_and_page()
    {
        var h = new PartyServiceTestHarness();
        await h.Service.CreateContactAsync(h.Ctx(h.Vendedor), Data("Ana"));

        var result = await h.Service.SearchAsync(new PartySearchCriteria(null, null, null, null), page: 0, pageSize: 100_000);

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
    }

    // ---------- Sin borrado físico ----------

    [Fact]
    public void The_application_exposes_no_delete_operation()
    {
        var methods = typeof(PartyService.Application.Parties.PartyManagementService).GetMethods().Select(m => m.Name);

        Assert.DoesNotContain(methods, name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase) || name.Contains("Remove", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(RealEstateCrm.BuildingBlocks.Persistence.IRepository<Party, Guid>).GetMethods().Select(m => m.Name), name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }

    private static PartyService.Application.Parties.PartyManagementService NewServiceWithValidator(PartyServiceTestHarness h, AuthorizationDecision decision) =>
        new(
            h.Parties,
            h.Relationships,
            new InMemoryPartyReadPort(h.Parties, h.Relationships),
            h.UserDirectory,
            new RealEstateCrm.TestSupport.Authorization.FakeResponsibleAssignmentValidationPort((_, _, _, _) => decision),
            h.Catalog,
            new RealEstateCrm.TestSupport.Persistence.PassthroughUnitOfWork(),
            h.Outbox,
            TimeProvider.System);
}

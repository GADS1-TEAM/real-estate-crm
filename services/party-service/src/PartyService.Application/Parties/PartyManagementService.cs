using PartyService.Application.Ports;
using PartyService.Domain;
using RealEstateCrm.BuildingBlocks.Authorization;
using RealEstateCrm.BuildingBlocks.Catalogs;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Events.Access;
using RealEstateCrm.Contracts.Events.Party;
using RealEstateCrm.Contracts.Paging;
using RealEstateCrm.Contracts.Parties;
using RealEstateCrm.Contracts.Users;

namespace PartyService.Application.Parties;

/// <summary>Datos de alta/edición de una Empresa o un Contacto. Solo <see cref="DisplayName"/> es obligatorio (captura mínima, alineado con crm-web).</summary>
public sealed record PartyDataInput(
    string DisplayName,
    string? LegalName,
    string? GivenNames,
    string? FamilyNames,
    string? TaxIdentifier,
    string? IdentityDocument,
    string? Email,
    string? Phone,
    string? Address,
    string? Industry,
    string? Notes,
    string? OriginCode);

/// <summary>
/// Application service de party-service: commands y queries de V2-PTY-001 (PTY-001..PTY-007).
/// </summary>
/// <remarks>
/// Los permisos genéricos (<c>parties.*</c>) los evalúa el controller con
/// <see cref="IAuthorizationPort"/>. Acá vive lo que solo el owner sabe (D2): la regla de
/// propiedad (el Administrador edita todo; el resto solo las parties donde es responsable), el
/// responsable inicial (= userId del creador) y la validación del responsable propuesto contra
/// access-service. Sin capa de mediator/CQRS (skill no habilitada en Wave 2).
/// </remarks>
public sealed class PartyManagementService(
    IRepository<Party, Guid> parties,
    IRepository<PartyRelationship, Guid> relationships,
    IPartyReadPort partyReads,
    IUserDirectoryPort userDirectory,
    IResponsibleAssignmentValidationPort responsibleValidation,
    ICatalogReaderPort catalogReader,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    TimeProvider timeProvider)
{
    /// <summary>Nombre del rol con alcance total sobre las parties. Coincide con el <c>RoleCode</c> de access-service.</summary>
    internal const string AdministratorRoleCode = "Administrador";

    // ---------- Commands ----------

    /// <summary>PTY-001: alta de Empresa (LEGAL_ENTITY), ACTIVE + POTENTIAL, responsable = creador.</summary>
    public Task<PartyDetailV1> CreateCompanyAsync(ExecutionContextV1 context, PartyDataInput input, CancellationToken cancellationToken = default) =>
        CreateAsync(context, PartyKind.LegalEntity, input, cancellationToken);

    /// <summary>PTY-003: alta de Contacto (NATURAL_PERSON), ACTIVE + POTENTIAL, responsable = creador.</summary>
    public Task<PartyDetailV1> CreateContactAsync(ExecutionContextV1 context, PartyDataInput input, CancellationToken cancellationToken = default) =>
        CreateAsync(context, PartyKind.NaturalPerson, input, cancellationToken);

    /// <summary>PTY-002: modifica una Empresa sin duplicar identidad.</summary>
    public Task<PartyDetailV1> UpdateCompanyAsync(ExecutionContextV1 context, Guid partyId, PartyDataInput input, CancellationToken cancellationToken = default) =>
        UpdateAsync(context, partyId, PartyKind.LegalEntity, input, cancellationToken);

    /// <summary>PTY-004: modifica un Contacto conservando sus relaciones.</summary>
    public Task<PartyDetailV1> UpdateContactAsync(ExecutionContextV1 context, Guid partyId, PartyDataInput input, CancellationToken cancellationToken = default) =>
        UpdateAsync(context, partyId, PartyKind.NaturalPerson, input, cancellationToken);

    /// <summary>
    /// PTY-005: relaciona un Contacto con una Empresa. Requiere ser responsable del Contacto (o
    /// Administrador); no modifica la Empresa.
    /// </summary>
    public async Task<PartyRelationshipV1> RelateContactToCompanyAsync(
        ExecutionContextV1 context,
        Guid contactId,
        Guid companyId,
        string relationshipType,
        CancellationToken cancellationToken = default)
    {
        var self = await RequireActiveSelfAsync(context, cancellationToken);

        if (!PartyWire.TryParseRelationshipType(relationshipType, out var type))
        {
            throw new PartyDomainException(PartyErrorCodes.InvalidRelationshipType, 422, $"'{relationshipType}' no es un tipo de relación válido ({string.Join(", ", RelationshipTypes.All)}).");
        }

        var contact = await RequireKindAsync(contactId, PartyKind.NaturalPerson, cancellationToken);
        var company = await RequireKindAsync(companyId, PartyKind.LegalEntity, cancellationToken);
        EnsureCanEdit(self, contact);

        if (await partyReads.RelationshipExistsAsync(contactId, companyId, type, cancellationToken))
        {
            throw new PartyDomainException(PartyErrorCodes.RelationshipAlreadyExists, 409, "Ya existe una relación vigente de ese tipo entre el contacto y la empresa.");
        }

        var now = timeProvider.GetUtcNow();
        var relationship = PartyRelationship.Create(contactId, companyId, type, context.ActorId, now);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await relationships.AddAsync(relationship, token);
            await EnqueueEventAsync(
                context,
                "PartyRelationshipCreated",
                relationship.RelationshipId,
                new PartyRelationshipCreatedV1(
                    relationship.RelationshipId,
                    relationship.FromPartyId,
                    relationship.ToPartyId,
                    PartyWire.ToWire(relationship.RelationshipType),
                    relationship.ValidFrom),
                token);
        }, cancellationToken);

        return ToRelationshipDto(relationship, viewedFrom: contactId, company);
    }

    /// <summary>
    /// PTY-007: cambia el estado comercial. La baja lógica es <c>INACTIVE</c> y la reactivación,
    /// un cambio explícito a otro estado; nada se borra. Requiere ser responsable (o Administrador).
    /// </summary>
    public async Task<PartyDetailV1> ChangeCommercialStatusAsync(
        ExecutionContextV1 context,
        Guid partyId,
        string commercialStatus,
        CancellationToken cancellationToken = default)
    {
        var self = await RequireActiveSelfAsync(context, cancellationToken);

        if (!PartyWire.TryParseCommercialStatus(commercialStatus, out var newStatus))
        {
            throw new PartyDomainException(PartyErrorCodes.InvalidCommercialStatus, 422, $"'{commercialStatus}' no es un estado comercial válido ({string.Join(", ", CommercialStatuses.All)}).");
        }

        var party = await RequirePartyAsync(partyId, cancellationToken);
        EnsureCanEdit(self, party);

        Party updated;
        try
        {
            updated = party.ChangeCommercialStatus(newStatus, context.ActorId, timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException ex)
        {
            throw new PartyDomainException(PartyErrorCodes.CommercialStatusUnchanged, 409, ex.Message);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await parties.UpdateAsync(updated, token);
            await EnqueueEventAsync(
                context,
                "PartyCommercialStatusChanged",
                updated.PartyId,
                new PartyCommercialStatusChangedV1(updated.PartyId, PartyWire.ToWire(party.CommercialStatus), PartyWire.ToWire(updated.CommercialStatus)),
                token);
        }, cancellationToken);

        return await BuildDetailAsync(updated, cancellationToken);
    }

    /// <summary>
    /// PTY-007/ASSIGN-001/ASSIGN-002: asigna o reasigna el responsable. El permiso lo verifica
    /// access-service (<see cref="IResponsibleAssignmentValidationPort"/>, que además comprueba que
    /// el responsable propuesto exista y esté ACTIVE); no exige ser el responsable actual. Publica
    /// <c>PartyResponsibleAssigned</c> con <see cref="ResponsibleAssignedV1"/>.
    /// </summary>
    public async Task<PartyDetailV1> AssignResponsibleAsync(
        ExecutionContextV1 context,
        Guid partyId,
        Guid responsibleUserId,
        CancellationToken cancellationToken = default)
    {
        var self = await RequireActiveSelfAsync(context, cancellationToken);
        var party = await RequirePartyAsync(partyId, cancellationToken);

        var decision = await responsibleValidation.ValidateAsync(context.ActorId, ResourceTypes.Party, partyId, responsibleUserId, cancellationToken);

        if (!decision.Allowed)
        {
            var reason = decision.ReasonCode ?? DenyReasons.PermissionNotGranted;

            throw reason is ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound or ResponsibleAssignmentDenyReasons.ResponsibleUserInactive
                ? new PartyDomainException(PartyErrorCodes.ResponsibleInvalid, 422, reason)
                : new PartyDomainException(ErrorCodes.Forbidden, 403, reason);
        }

        Party updated;
        try
        {
            updated = party.AssignResponsible(responsibleUserId, context.ActorId, timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException ex)
        {
            throw new PartyDomainException(PartyErrorCodes.ResponsibleUnchanged, 409, ex.Message);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await parties.UpdateAsync(updated, token);
            await EnqueueEventAsync(
                context,
                "PartyResponsibleAssigned",
                updated.PartyId,
                new ResponsibleAssignedV1(ResourceTypes.Party, updated.PartyId, party.ResponsibleUserId, responsibleUserId, self.UserId),
                token);
        }, cancellationToken);

        return await BuildDetailAsync(updated, cancellationToken);
    }

    // ---------- Queries ----------

    /// <summary>SearchParties (paginado). Orden estable por nombre.</summary>
    public async Task<PageV1<PartySummaryV1>> SearchAsync(
        PartySearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await partyReads.SearchAsync(criteria, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), cancellationToken);

        return new PageV1<PartySummaryV1>(result.Items.Select(ToSummary).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    /// <summary>Detalle de cualquier Party (Empresa o Contacto), para paneles que no saben el tipo de antemano.</summary>
    public async Task<PartyDetailV1> GetPartyDetailAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        await BuildDetailAsync(await RequirePartyAsync(partyId, cancellationToken), cancellationToken);

    /// <summary>GetCompanyDetail (PTY-006): incluye los contactos relacionados.</summary>
    public async Task<PartyDetailV1> GetCompanyDetailAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        await BuildDetailAsync(await RequireKindAsync(partyId, PartyKind.LegalEntity, cancellationToken), cancellationToken);

    /// <summary>GetContactDetail (PTY-006): incluye las empresas relacionadas (ninguna si es individual).</summary>
    public async Task<PartyDetailV1> GetContactDetailAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        await BuildDetailAsync(await RequireKindAsync(partyId, PartyKind.NaturalPerson, cancellationToken), cancellationToken);

    // ---------- Internals ----------

    private async Task<PartyDetailV1> CreateAsync(ExecutionContextV1 context, PartyKind kind, PartyDataInput input, CancellationToken cancellationToken)
    {
        var self = await RequireActiveSelfAsync(context, cancellationToken);
        var (originCode, originVersion) = await ResolveOriginAsync(input.OriginCode, existingCode: null, existingVersion: null, cancellationToken);

        var party = Party.Register(kind, ToProfile(kind, input), self.UserId, originCode, originVersion, context.ActorId, timeProvider.GetUtcNow());

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await parties.AddAsync(party, token);
            await EnqueueEventAsync(
                context,
                "PartyRegistered",
                party.PartyId,
                new PartyRegisteredV1(
                    party.PartyId,
                    PartyWire.ToWire(party.Kind),
                    party.Profile.DisplayName,
                    PartyWire.ToWire(party.IdentityStatus),
                    PartyWire.ToWire(party.CommercialStatus),
                    party.ResponsibleUserId,
                    party.OriginCode,
                    party.OriginCatalogVersion),
                token);
        }, cancellationToken);

        return ToDetail(party, Array.Empty<PartyRelationshipV1>());
    }

    private async Task<PartyDetailV1> UpdateAsync(ExecutionContextV1 context, Guid partyId, PartyKind kind, PartyDataInput input, CancellationToken cancellationToken)
    {
        var self = await RequireActiveSelfAsync(context, cancellationToken);
        var party = await RequireKindAsync(partyId, kind, cancellationToken);
        EnsureCanEdit(self, party);

        var (originCode, originVersion) = await ResolveOriginAsync(input.OriginCode, party.OriginCode, party.OriginCatalogVersion, cancellationToken);
        var updated = party.UpdateData(ToProfile(kind, input), originCode, originVersion, context.ActorId, timeProvider.GetUtcNow());
        var changedFields = party.ChangedFields(updated);

        if (changedFields.Count == 0)
        {
            return await BuildDetailAsync(party, cancellationToken);
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await parties.UpdateAsync(updated, token);
            await EnqueueEventAsync(
                context,
                "PartyUpdated",
                updated.PartyId,
                new PartyUpdatedV1(updated.PartyId, PartyWire.ToWire(updated.Kind), updated.Profile.DisplayName, changedFields),
                token);
        }, cancellationToken);

        return await BuildDetailAsync(updated, cancellationToken);
    }

    /// <summary>
    /// Valida <c>originCode</c> contra <c>CommercialOrigin</c> (D7, solo entradas activas) y
    /// devuelve el <c>catalogVersion</c> a guardar. Si el código no cambió, se conserva la versión
    /// original: un origen dado de baja después no invalida un registro histórico.
    /// </summary>
    private async Task<(string? Code, int? Version)> ResolveOriginAsync(string? originCode, string? existingCode, int? existingVersion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(originCode))
        {
            return (null, null);
        }

        if (string.Equals(originCode, existingCode, StringComparison.Ordinal))
        {
            return (existingCode, existingVersion);
        }

        var catalog = await catalogReader.GetAsync(CatalogTypes.CommercialOrigin, activeOnly: true, cancellationToken);

        if (!catalog.Entries.Any(entry => entry.Active && string.Equals(entry.Code, originCode, StringComparison.Ordinal)))
        {
            throw new PartyDomainException(PartyErrorCodes.InvalidOrigin, 422, $"'{originCode}' no es un origen comercial activo.");
        }

        return (originCode, catalog.CatalogVersion);
    }

    private async Task<UserSelfV1> RequireActiveSelfAsync(ExecutionContextV1 context, CancellationToken cancellationToken)
    {
        var self = await userDirectory.GetSelfAsync(context.ActorId, cancellationToken);

        return self.User
            ?? throw new PartyDomainException(ErrorCodes.Forbidden, 403, self.DenyReasonCode ?? DenyReasons.PermissionNotGranted);
    }

    /// <summary>Regla de propiedad (D2): Administrador edita todo; el resto, solo donde es responsable.</summary>
    private static void EnsureCanEdit(UserSelfV1 self, Party party)
    {
        if (string.Equals(self.RoleCode, AdministratorRoleCode, StringComparison.Ordinal) || party.ResponsibleUserId == self.UserId)
        {
            return;
        }

        throw new PartyDomainException(ErrorCodes.Forbidden, 403, PartyErrorCodes.NotResponsibleReason);
    }

    private async Task<Party> RequirePartyAsync(Guid partyId, CancellationToken cancellationToken) =>
        await parties.GetByIdAsync(partyId, cancellationToken)
        ?? throw new PartyDomainException(PartyErrorCodes.PartyNotFound, 404, $"No existe la party '{partyId}'.");

    /// <summary>Una Empresa pedida como Contacto (o al revés) es inexistente para ese recurso: 404.</summary>
    private async Task<Party> RequireKindAsync(Guid partyId, PartyKind kind, CancellationToken cancellationToken)
    {
        var party = await RequirePartyAsync(partyId, cancellationToken);

        return party.Kind == kind
            ? party
            : throw new PartyDomainException(PartyErrorCodes.PartyNotFound, 404, $"No existe {(kind == PartyKind.LegalEntity ? "la empresa" : "el contacto")} '{partyId}'.");
    }

    private async Task<PartyDetailV1> BuildDetailAsync(Party party, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var current = (await partyReads.ListRelationshipsAsync(party.PartyId, cancellationToken))
            .Where(relationship => relationship.IsCurrent(now))
            .ToList();

        var relatedIds = current
            .Select(relationship => relationship.FromPartyId == party.PartyId ? relationship.ToPartyId : relationship.FromPartyId)
            .Distinct()
            .ToList();

        var related = (await partyReads.GetManyAsync(relatedIds, cancellationToken)).ToDictionary(p => p.PartyId);

        var dtos = current
            .Select(relationship =>
            {
                var otherId = relationship.FromPartyId == party.PartyId ? relationship.ToPartyId : relationship.FromPartyId;
                return ToRelationshipDto(relationship, party.PartyId, related.GetValueOrDefault(otherId));
            })
            .ToList();

        return ToDetail(party, dtos);
    }

    private static PartyProfile ToProfile(PartyKind kind, PartyDataInput input) =>
        kind == PartyKind.LegalEntity
            ? new PartyProfile(
                Clean(input.DisplayName)!, Clean(input.LegalName), null, null, Clean(input.TaxIdentifier), null,
                Clean(input.Email), Clean(input.Phone), Clean(input.Address), Clean(input.Industry), Clean(input.Notes))
            : new PartyProfile(
                Clean(input.DisplayName)!, null, Clean(input.GivenNames), Clean(input.FamilyNames), Clean(input.TaxIdentifier), Clean(input.IdentityDocument),
                Clean(input.Email), Clean(input.Phone), Clean(input.Address), null, Clean(input.Notes));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PartySummaryV1 ToSummary(Party party) =>
        new(
            party.PartyId,
            PartyWire.ToWire(party.Kind),
            party.Profile.DisplayName,
            party.Profile.Email,
            party.Profile.Phone,
            PartyWire.ToWire(party.IdentityStatus),
            PartyWire.ToWire(party.CommercialStatus),
            party.ResponsibleUserId,
            party.OriginCode);

    private static PartyRelationshipV1 ToRelationshipDto(PartyRelationship relationship, Guid viewedFrom, Party? other) =>
        new(
            relationship.RelationshipId,
            relationship.FromPartyId,
            relationship.ToPartyId,
            PartyWire.ToWire(relationship.RelationshipType),
            relationship.ValidFrom,
            relationship.ValidTo,
            relationship.CreatedBy,
            relationship.FromPartyId == viewedFrom ? relationship.ToPartyId : relationship.FromPartyId,
            other?.Profile.DisplayName);

    private static PartyDetailV1 ToDetail(Party party, IReadOnlyList<PartyRelationshipV1> relationshipDtos) =>
        new(
            party.PartyId,
            PartyWire.ToWire(party.Kind),
            party.Profile.DisplayName,
            party.Profile.LegalName,
            party.Profile.GivenNames,
            party.Profile.FamilyNames,
            party.Profile.TaxIdentifier,
            party.Profile.IdentityDocument,
            party.Profile.Email,
            party.Profile.Phone,
            party.Profile.Address,
            party.Profile.Industry,
            PartyWire.ToWire(party.IdentityStatus),
            PartyWire.ToWire(party.CommercialStatus),
            party.ResponsibleUserId,
            party.OriginCode,
            party.OriginCatalogVersion,
            party.Profile.Notes,
            party.CreatedAt,
            party.CreatedBy,
            party.UpdatedAt,
            party.UpdatedBy,
            party.CommercialStatusChangedAt,
            party.CommercialStatusChangedBy,
            party.Version,
            relationshipDtos);

    private Task EnqueueEventAsync<TPayload>(ExecutionContextV1 context, string name, Guid aggregateId, TPayload payload, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var envelope = new EventEnvelopeV1<TPayload>(
            EventId: Guid.NewGuid(),
            Name: name,
            Version: 1,
            OccurredAt: now,
            ActorId: context.ActorId,
            CorrelationId: context.CorrelationId,
            CausationId: context.CausationId,
            AggregateId: aggregateId,
            Payload: payload);

        return outbox.EnqueueAsync(OutboxMessage.From(envelope, now), cancellationToken);
    }
}

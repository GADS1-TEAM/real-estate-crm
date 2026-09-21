using PartyService.Application.Parties;
using PartyService.Application.Ports;
using PartyService.Domain;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Catalogs;
using RealEstateCrm.Contracts.Context;
using RealEstateCrm.Contracts.Paging;
using RealEstateCrm.TestSupport.Authorization;
using RealEstateCrm.TestSupport.Catalogs;
using RealEstateCrm.TestSupport.Messaging;
using RealEstateCrm.TestSupport.Persistence;

namespace PartyService.Application.Tests.Fakes;

/// <summary>Un actor de prueba: su <c>sub</c> de Keycloak, su <c>userId</c> de access-service y su rol.</summary>
public sealed record TestActor(Guid Subject, Guid UserId, string? RoleCode);

/// <summary>
/// Wiring en memoria de <see cref="PartyManagementService"/>. Los tres actores reproducen los dos
/// esquemas de id del sistema: el <c>sub</c> (actorId del contexto) y el <c>userId</c> propio de
/// access-service (responsibleUserId), siempre distintos.
/// </summary>
public sealed class PartyServiceTestHarness
{
    public TestActor Admin { get; } = NewActor("Administrador");

    public TestActor Vendedor { get; } = NewActor("Vendedor");

    public TestActor OtherVendedor { get; } = NewActor("Vendedor");

    public TestActor Responsable { get; } = NewActor("Responsable Comercial");

    public InMemoryRepository<Party, Guid> Parties { get; } = new(p => p.PartyId);

    public InMemoryRepository<PartyRelationship, Guid> Relationships { get; } = new(r => r.RelationshipId);

    public InMemoryOutbox Outbox { get; } = new();

    public FakeUserDirectoryPort UserDirectory { get; } = new();

    public FakeCatalogReaderPort Catalog { get; } = new FakeCatalogReaderPort()
        .WithEntry(CatalogTypes.CommercialOrigin, "REFERIDO")
        .WithEntry(CatalogTypes.CommercialOrigin, "WHATSAPP")
        .WithEntry(CatalogTypes.CommercialOrigin, "ORIGEN_VIEJO", active: false);

    /// <summary>Usuarios que access-service considera válidos como responsable (por userId).</summary>
    public HashSet<Guid> ActiveResponsibleCandidates { get; } = new();

    public PartyManagementService Service { get; }

    public PartyServiceTestHarness()
    {
        foreach (var actor in new[] { Admin, Vendedor, OtherVendedor, Responsable })
        {
            UserDirectory.WithActiveUser(actor.Subject, actor.UserId, actor.RoleCode);
            ActiveResponsibleCandidates.Add(actor.UserId);
        }

        var validation = new FakeResponsibleAssignmentValidationPort((actorSubject, _, _, responsibleUserId) =>
            ActiveResponsibleCandidates.Contains(responsibleUserId)
                ? AuthorizationDecision.Allow()
                : AuthorizationDecision.Deny(ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound));

        Service = new PartyManagementService(
            Parties,
            Relationships,
            new InMemoryPartyReadPort(Parties, Relationships),
            UserDirectory,
            validation,
            Catalog,
            new PassthroughUnitOfWork(),
            Outbox,
            TimeProvider.System);
    }

    public ExecutionContextV1 Ctx(TestActor actor, Guid? correlationId = null) =>
        new(actor.Subject, "Actor de prueba", "actor@crm-dev.local", new[] { actor.RoleCode ?? "none" }, Array.Empty<string>(), correlationId ?? Guid.NewGuid(), null);

    public static PartyDataInput Data(string name, string? email = null, string? phone = null, string? taxId = null, string? origin = null) =>
        new(name, null, null, null, taxId, null, email, phone, null, null, null, origin);

    private static TestActor NewActor(string role) => new(Guid.NewGuid(), Guid.NewGuid(), role);
}

/// <summary>Búsqueda en memoria equivalente a la de Mongo (texto sin distinguir mayúsculas, orden por nombre).</summary>
internal sealed class InMemoryPartyReadPort(
    InMemoryRepository<Party, Guid> parties,
    InMemoryRepository<PartyRelationship, Guid> relationships) : IPartyReadPort
{
    public Task<PagedResult<Party>> SearchAsync(PartySearchCriteria criteria, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<Party> query = parties.All;

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var text = criteria.Query.Trim();
            query = query.Where(p =>
                Contains(p.Profile.DisplayName, text) || Contains(p.Profile.Email, text) || Contains(p.Profile.Phone, text) || Contains(p.Profile.TaxIdentifier, text));
        }

        if (criteria.Kind is { } kind)
        {
            query = query.Where(p => p.Kind == kind);
        }

        if (criteria.CommercialStatus is { } status)
        {
            query = query.Where(p => p.CommercialStatus == status);
        }

        if (criteria.ResponsibleUserId is { } responsible)
        {
            query = query.Where(p => p.ResponsibleUserId == responsible);
        }

        var ordered = query.OrderBy(p => p.Profile.DisplayName, StringComparer.Ordinal).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<Party>(items, page, pageSize, ordered.Count, page * pageSize < ordered.Count));
    }

    public Task<IReadOnlyList<Party>> GetManyAsync(IReadOnlyCollection<Guid> partyIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Party>>(parties.All.Where(p => partyIds.Contains(p.PartyId)).ToList());

    public Task<IReadOnlyList<PartyRelationship>> ListRelationshipsAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PartyRelationship>>(relationships.All.Where(r => r.FromPartyId == partyId || r.ToPartyId == partyId).ToList());

    public Task<bool> RelationshipExistsAsync(Guid contactId, Guid companyId, RelationshipType type, CancellationToken cancellationToken = default) =>
        Task.FromResult(relationships.All.Any(r => r.FromPartyId == contactId && r.ToPartyId == companyId && r.RelationshipType == type && r.ValidTo is null));

    private static bool Contains(string? value, string text) => value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;
}

using PartyService.Domain;
using RealEstateCrm.Contracts.Paging;

namespace PartyService.Application.Ports;

/// <summary>Filtros de <c>SearchParties</c>. Todos opcionales.</summary>
/// <param name="Query">Texto libre: coincide (sin distinguir mayúsculas) con nombre, email, teléfono o CUIT/identificador fiscal.</param>
public sealed record PartySearchCriteria(
    string? Query,
    PartyKind? Kind,
    CommercialStatus? CommercialStatus,
    Guid? ResponsibleUserId);

/// <summary>
/// Puerto de lectura de party-service, complementario a <c>IRepository</c> (que solo resuelve por
/// id): búsqueda paginada y consultas de relaciones. Sin ninguna garantía de unicidad.
/// </summary>
public interface IPartyReadPort
{
    Task<PageV1<Party>> SearchAsync(PartySearchCriteria criteria, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Party>> GetManyAsync(IReadOnlyCollection<Guid> partyIds, CancellationToken cancellationToken = default);

    /// <summary>Relaciones (en cualquier extremo) de una Party.</summary>
    Task<IReadOnlyList<PartyRelationship>> ListRelationshipsAsync(Guid partyId, CancellationToken cancellationToken = default);

    Task<bool> RelationshipExistsAsync(Guid contactId, Guid companyId, RelationshipType type, CancellationToken cancellationToken = default);
}
